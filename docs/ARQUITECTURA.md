# Survival PvP para CrazyGames: arquitectura técnica

Juego de supervivencia PvP en mundo abierto (estilo Rust/ARK sin dinosaurios)
para navegador (WebGL) y publicación en CrazyGames.

> Comprueba los límites concretos (tamaño de build, normas de anuncios,
> requisitos de multijugador) en la documentación de desarrolladores de
> CrazyGames antes de enviar el juego. Cambian con el tiempo.

---

## 1. Stack tecnológico

| Capa | Elección | Por qué |
|---|---|---|
| Motor | **Unity 6 LTS** | El mejor soporte WebGL del mercado, IL2CPP, Addressables, buen profiler. |
| Render pipeline | **URP, ruta Forward** | El único pipeline viable en WebGL 2. HDRP no soporta WebGL. El Built-in está en desuso. |
| Red | **FishNet + transporte Bayou (WebSockets)** | Gratis y open source. Servidor autoritativo con predicción del cliente. Interest management por distancia/grid, que es imprescindible en mundo abierto. |
| Servidor de juego | **Build Linux headless (Dedicated Server) de Unity** | El cliente WebGL no puede ser host: hacen falta servidores dedicados. |
| Hosting | VPS (Hetzner/OVH) para el MVP y **Edgegap** para escalar | Un VPS de unos 10–20 €/mes aguanta 1–2 servidores de 40 jugadores. Edgegap tiene integración con FishNet. |
| TLS | Caddy o nginx delante del servidor (**wss://**) | CrazyGames sirve por HTTPS, así que el navegador bloquea `ws://`. |
| Identidad | **Cuenta de usuario del SDK de CrazyGames** (token JWT) + invitado | Login sin fricción. El servidor verifica el token. |
| Backend persistente | **Postgres (Supabase)** para perfiles/estadísticas. Estado del mundo en snapshots del propio servidor | Las bases y el mapa los guarda el servidor (archivo binario cada N minutos). El backend solo guarda lo que sobrevive a los wipes. |
| SDK | **CrazyGames Unity SDK v3** | Anuncios, eventos de gameplay, cuentas, invitaciones. |
| CI | GameCI (GitHub Actions) | Builds WebGL y servidor reproducibles. |

**¿Por qué no Photon Fusion?** Funciona en WebGL, pero su modo Shared es
autoritativo por cliente, lo que en PvP con botín significa trampas. El modo
Server de Fusion también necesita servidores dedicados y además cobra por
CCU. **Mirror** es válido, pero FishNet trae predicción e interest
management más maduros. **Netcode for GameObjects** está más verde en
mundos grandes.

**Regla de oro de red:** el cliente solo envía *intenciones* ("golpeo hacia
aquí", "crafteo la receta 12", "coloco la pieza 3 aquí"). El servidor valida
y ejecuta. Los scripts de `Assets/_Project/Scripts` están en C# puro para
poder ejecutarse en el servidor sin cambios.

Configuración WebGL del Player:
- IL2CPP, *Code Optimization: Disk Size with LTO*, *Managed Stripping: High*, *Strip Engine Code*
- Compresión **Brotli** y *Decompression Fallback* desactivado si CrazyGames sirve las cabeceras correctas
- *Exceptions: Explicitly Thrown Only*
- Texturas ASTC (móvil) / DXT‑BC (escritorio)
- Memoria inicial de 256–512 MB y crecimiento geométrico. Objetivo: menos de 1 GB de heap en escritorio.
- `Application.targetFrameRate = -1`: en WebGL manda el `requestAnimationFrame` del navegador.

---

## 2. Optimización gráfica: "se ve bien" con 60 FPS en navegador

La clave es que el estilo artístico haga el trabajo, no la fuerza bruta.
**Estilo estilizado/semirrealista** (texturas pintadas, paleta cuidada) en
lugar de PBR fotorrealista: se ve mejor a baja resolución y pesa menos.

**Mapa**
- Una **isla de unos 1×1 km** para el MVP, en 4–16 chunks de terreno.
- **Niebla de distancia** atmosférica: esconde el *far clip* (~300–400 m) y da profundidad. Es el truco número 1.
- Terreno con **máximo 4 capas** (una sola pasada de shader), *Pixel Error* 8–10 y *Basemap Distance* ~150 m.
- Hierba con GPU Instancing a 30–40 m como máximo, con densidad según el nivel de calidad.

**Iluminación**
- Un único **Directional Light** en tiempo real, con 2 cascadas y *Shadow Distance* de 40–60 m.
- Ciclo día/noche simple (rotar el sol y cambiar gradientes de ambiente/niebla), sin GI en tiempo real.
- **Light Probes** horneadas para interiores y cuevas.
- Para no depender de GI: AO horneado en las texturas y vértices de los props.
- Luces puntuales (antorchas, hogueras): *Per Object Limit* de 2–4, sin sombras y apagadas por distancia.

**Post-proceso (un solo Volume global)**
- Activado: Tonemapping (Neutral/ACES), Color Adjustments, Bloom en baja calidad y Vignette.
- Desactivado: **SSAO**, Motion Blur, DoF, SSR y la ruta Deferred.

**Geometría y culling**
- **LOD Group** en todo: árboles en 3 LODs + **impostor/billboard**; rocas en 2–3 LODs. Sin crossfade, porque el dither cuesta.
- **`Camera.layerCullDistances`** por capa: props pequeños a 60 m, árboles a 300 m, construcciones a 250 m. Es gratis y muy eficaz.
- **Occlusion Culling** horneado solo para la geometría estática grande (acantilados, cuevas). Las bases son dinámicas y no se benefician.
- El **interest management** de FishNet hace de "culling de red": a más de ~150 m ni existen en el cliente los jugadores, las bolsas o las piezas pequeñas.
- **SRP Batcher** activo, pocos shaders, **atlas de texturas** compartido por los props y los materiales de construcción (un material por tier).
- GPU Resident Drawer y cualquier cosa que use compute shaders **no funcionan en WebGL 2**. Puedes probarlos más adelante si apuntas a WebGPU.

**Memoria y carga**
- **Addressables**: la escena inicial (menú + SDK) debería pesar menos de 10–15 MB. La isla se descarga después, con barra de progreso.
- Texturas a 1K por defecto (2K solo para el terreno y el héroe), con *mipmap streaming*.
- Audio en Vorbis con calidad 50–70 %, música en *Streaming* y SFX en *Compressed In Memory*.
- **Object pooling** para proyectiles, VFX de impacto, bolsas de botín y números de daño. No hagas `Instantiate`/`Destroy` en combate.
- Cero asignaciones por frame en el gameplay: buffers reutilizados, `NonAlloc` y nada de LINQ en `Update`. El GC de WebGL solo corre entre frames y los picos se notan.

**Calidad adaptativa:** 3 niveles (Bajo/Medio/Alto) autodetectados por el
FPS de los primeros 10 s. Cambian la escala de render (URP *Render Scale*
0.75–1.0), la distancia de sombras, la densidad de hierba y el sesgo de LOD.

---

## 3. Arquitectura de datos: crafteo, progresión y construcción

Todo lo que define el juego es **dato** (ScriptableObjects). El código solo
implementa reglas genéricas. Añadir "bronce" entre piedra y hierro es crear
assets, no escribir código.

```
ItemDefinition (SO)              id, nombre, icono, categoría, TechTier, maxStack
 └─ ToolDefinition (SO)          ToolType, gatherPower, yieldMultiplier, durabilidad, daño
RecipeDefinition (SO)            ingredientes[], resultado, tiempo, CraftingStation, TechTier, desbloqueo
ResourceNodeDefinition (SO)      herramientas eficaces, tier mínimo, vida, yields[], bonus, respawn
BuildingPieceDefinition (SO)     tipo (cimiento/pared/...), prefab, vida base, unidades de coste
BuildingTierDefinition (SO)      Twig/Wood/Stone/Metal: ×vida, coste por unidad, resistencias por DamageType
ItemDatabase (SO)                id → asset (red y guardado solo usan ints)
```

**Progresión (tres ejes que se refuerzan):**
1. **`TechTier`** en ítems y recetas: Primitive → Stone → Iron → Steel.
2. **`CraftingStation`**: la mano, luego hoguera, banco, **horno** (mineral → lingote) y **yunque** (armas y herramientas de hierro).
3. **Puertas de recolección**: el `ResourceNodeDefinition.minToolTier` de una veta de hierro exige pico de piedra. Así no hay hierro sin pasar por la piedra.

Cadena de ejemplo:
```
Madera + Piedra   ─(mano)─────────► Hacha de piedra / Pico de piedra   [Stone]
Madera + Fibra    ─(banco)────────► Arco + Flechas                     [Stone]
Pico de piedra    ─(veta hierro)──► Mineral de hierro
Mineral + Madera  ─(horno)────────► Lingote de hierro
Lingotes + Madera ─(yunque)───────► Hacha/Espada/Pico de hierro         [Iron]
```

**Construcción:** pieza × material. Una pared es un `BuildingPieceDefinition`.
Sus stats salen de `baseHealth × tier.HealthMultiplier` y su coste de
`tier.CostPerUnit × costUnits`. Mejorar de madera a piedra solo cambia la
referencia al tier y el material. El asalto usa `DamageType` y las
resistencias del tier:
- La madera es débil al fuego.
- La piedra es casi inmune a las flechas y vulnerable a los picos.
- El metal solo cae con explosivos.

Colocación por **sockets** (Transforms hijos en el prefab) y una rejilla
lógica. El servidor valida solapamiento, apoyo (cimiento en suelo, pared
sobre cimiento) y **Tool Cupboard** (zona de privilegio de construcción).

Estado de partida (inventarios, vida de piezas) = estructuras C# planas con
IDs. Nunca se guarda estado en los ScriptableObjects.

---

## 4. Scripts base incluidos

`Assets/_Project/Scripts/` (assembly `Survival.Runtime`):

| Archivo | Rol |
|---|---|
| `Core/Enums.cs` | `TechTier`, `ToolType`, `ItemCategory`, `CraftingStation` |
| `Items/ItemDefinition.cs`, `ToolDefinition.cs` | Datos de ítems y herramientas |
| `Items/ItemStack.cs` | `ItemStack` (slot) e `ItemAmount` (coste) |
| `Items/ItemDatabase.cs` | Lookup por ID y detección de IDs duplicados |
| `Inventory/Inventory.cs` | Inventario por slots en C# puro: apilar, quitar atómico, durabilidad, mover, clonar |
| `Inventory/InventoryComponent.cs` | Envoltorio MonoBehaviour |
| `Crafting/RecipeDefinition.cs` | Receta |
| `Crafting/CraftingSystem.cs` | Validación y crafteo atómico (simula sobre una copia) |
| `Crafting/CraftingQueue.cs` | Cola con tiempo, cobro al encolar y reembolso al cancelar |
| `Crafting/PlayerCraftingContext.cs`, `CraftingStationMarker.cs` | Desbloqueos, tier y estaciones cercanas |
| `Gathering/ResourceNodeDefinition.cs` | Tipo de árbol/roca/veta |
| `Gathering/ResourceNode.cs` | Nodo: botín proporcional al daño, sin pérdida por redondeo, respawn sin coste de `Update` |
| `Gathering/PlayerHarvester.cs` | SphereCast desde la cámara, cooldown, desgaste, overflow |
| `Building/*.cs` | Datos de piezas y tiers de construcción |

**Montaje rápido en Unity**
1. Crea los assets: `Create > Survival > Items > Item` (Madera, Piedra) y `Create > Survival > Items > Tool`. Crea "Puños" (`ToolType.Hand`, durabilidad 0) y "Hacha de piedra".
2. Crea `Create > Survival > Gathering > Resource Node` para el árbol: `effectiveTools = Axe`, `yields = Madera × 60`.
3. Añade a un prefab de árbol `ResourceNode` (con `visualRoot` = la malla y un collider).
4. Añade al jugador `InventoryComponent`, `PlayerCraftingContext`, `CraftingQueue` y `PlayerHarvester` (asigna los Puños).
5. Pon `EquippedSlot` desde tu hotbar y conecta `Overflow` a un spawner de bolsas.

**Paso a red (FishNet), esquema:**
```csharp
// En el NetworkBehaviour del jugador
[ServerRpc] private void CmdSwing(Vector3 origin, Vector3 dir)
{
    // Valida que origin está cerca de la posición del jugador en el servidor
    harvester.TrySwing(new Ray(origin, dir)); // misma lógica, ahora autoritativa
}
// Inventario: replica los slots con SyncList<NetItemSlot{ushort itemId; ushort amount; ushort durability}>
// y un TargetRpc solo para el dueño (los demás jugadores no necesitan ver tu mochila).
// ResourceNode: SyncVar<bool> depleted, observado por distancia.
```

---

## 5. Hoja de ruta para el MVP (8 semanas)

Objetivo: isla de 1 km² con 20–40 jugadores por servidor. El jugador
recolecta, craftea hasta hierro, construye una base de madera/piedra, la
defiende o asalta otras, y todo funciona en CrazyGames a 60 FPS en un
portátil medio.

**Semana 1: cimientos y pipeline**
- Unity 6 LTS + URP con los ajustes WebGL de la sección 1.
- GameCI genera la build WebGL; súbela al portal de desarrolladores de CrazyGames (vista previa) **el primer día**, con una escena vacía.
- Controlador en primera persona y un sistema de input con el Input System.
- Medición base de FPS y memoria en Chrome y Firefox.

**Semana 2: núcleo en un jugador**
- Integra estos scripts: inventario, hotbar UI, recolección y crafteo con cola.
- Primeros 10 ítems: madera, piedra, fibra, hacha, pico, lanza, arco, flecha, hoguera y banco.
- Tests de EditMode para `Inventory` y `CraftingSystem`.

**Semana 3: multijugador vertical (la parte de más riesgo, así que va pronto)**
- FishNet + Bayou: servidor Linux headless en un VPS con Caddy (wss).
- Movimiento con predicción, recolección e inventario autoritativos.
- 2 builds de navegador jugando juntas en la isla de pruebas.

**Semana 4: la isla**
- Terreno de 1 km² con 4 capas, niebla, LODs, impostores y `layerCullDistances`.
- Spawner de nodos (árboles, rocas, vetas) con densidad por bioma.
- Interest management por grid.
- Prueba de rendimiento con 30 jugadores simulados (bots en el servidor).

**Semana 5: construcción**
- Colocación con sockets y preview fantasma, validación en el servidor.
- Tiers Twig/Wood/Stone con mejora con martillo, puertas con candado y Tool Cupboard.
- Guardado y carga del mundo en el servidor.

**Semana 6: PvP y asalto**
- Salud, cuerpo a cuerpo, arco (proyectil simulado en el servidor) y lag compensation básica de FishNet.
- Muerte, bolsa de botín, respawn y saco de dormir.
- Daño a estructuras por `DamageType`.
- Anti-cheat elemental: validación de rango, cooldown y velocidad en el servidor.

**Semana 7: progresión y SDK**
- Horno, yunque, mineral → lingote y herramientas y armas de hierro. Tier Metal en construcción.
- Integración del SDK de CrazyGames:
  - eventos `loadingStart/Stop` y `gameplayStart/Stop`;
  - *midgame ad* solo en pausas naturales (pantalla de muerte/respawn);
  - cuenta de usuario;
  - enlace de invitación.
- Selección de servidor (el de menos latencia con plazas libres) y wipe semanal.

**Semana 8: pulido y envío**
- Pase de profiler en un portátil de gama media (Chrome) y ajuste de los 3 niveles de calidad.
- Recorte del tamaño de la build y Addressables para la isla.
- Tutorial de 60 s (talar, craftear un hacha, construir un cimiento), ajustes de sensibilidad y FOV.
- Playtest cerrado con 20+ personas y envío a CrazyGames.

**Fuera del MVP:** mapa más grande, animales/IA, armas de fuego,
explosivos avanzados, clanes, vehículos y WebGPU.
