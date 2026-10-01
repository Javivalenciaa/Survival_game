# Isla de Hierro

Survival PvP de mundo abierto para navegador, pensado para CrazyGames.

## Versión jugable (navegador, sin instalar nada)

- **Jugar online:** https://javivalenciaa.github.io/Survival_game/ (GitHub Pages, rama `main`).
- **Descargar:** `web/isla-de-hierro.html` es el juego en un solo archivo; ábrelo con doble clic.
- `index.html` (raíz) es la fuente del juego. Necesita conexión para cargar Three.js desde cdnjs.

### Gráficos
- Render HDR propio: bloom, tonemapping ACES, corrección de color, viñeta y antialiasing MSAA.
- Cielo atmosférico con sol, luna, estrellas y nubes animadas. Iluminación ambiental y reflejos generados a partir del cielo.
- Terreno con shader de texturas procedurales (hierba, bosque, tierra, arena, roca triplanar y nieve) mezcladas por altura, pendiente y bioma, con relieve de detalle y cáusticas bajo el agua.
- Hierba en GPU: hasta 110 000 briznas con viento, flores y que se apartan al pasar.
- Agua con color por profundidad, espuma en la orilla, ondas de detalle y reflejos.
- Árboles con copas compuestas que se mueven con el viento (pinos, robles, abedules y manzanos) y rocas con forma natural.
- Calidad gráfica Baja/Media/Alta/Ultra, con ajuste automático si los FPS caen.

### Física y sistemas
- Energía para correr y saltar, agacharse con sigilo, daño por caída, resbalones en pendientes y sacudidas de cámara.
- Árboles que caen al talarlos, rocas que estallan en trozos que rebotan y construcciones que se derrumban.
- Flotación, buceo y oxígeno, hambre y sed, caza y despiece, hoguera y odre.
- Clima variable: nubes, lluvia y tormentas con rayos. Se puede beber la lluvia.

### Mapa
- Isla de 640 × 640 m con dos cordilleras de cumbres nevadas, hasta cuatro lagos, bosques y praderas.
- Ruinas, campamentos y un faro con cajas de suministros que se rellenan. Mapa con la tecla M y brújula.
- Construcción modular, saqueadores nocturnos, lobos, ciclo de día y noche y guardado automático.

## Base para Unity (alternativa)

- Arquitectura, optimización y roadmap: [`docs/ARQUITECTURA.md`](docs/ARQUITECTURA.md)
- Scripts de C#: copia `Assets/_Project` dentro de un proyecto de Unity 6 (URP).
