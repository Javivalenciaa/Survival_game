# Isla de Hierro

Survival PvP de mundo abierto para navegador, pensado para CrazyGames.

## Versión jugable (navegador, sin instalar nada)

`web/isla-de-hierro.html` es el juego en un solo archivo: descárgalo y ábrelo
con doble clic. `web/index.html` es la misma versión sin la cabecera HTML. Es un prototipo completo en JavaScript + Three.js. Se
abre en cualquier navegador de ordenador y se juega con teclado y ratón.
Incluye la isla con lagos de agua dulce y mar con oleaje (flotación, buceo y
oxígeno), hambre y sed, frutas (bayas y manzanas), caza (ciervos, jabalíes y
lobos agresivos) con despiece y hoguera para asar, la fabricación hasta la
edad del hierro, la construcción modular (cimientos, paredes, puertas, techos
con mejoras de paja a metal), los saqueadores nocturnos, el ciclo de día y
noche y el guardado automático.

Para probarlo en local, sírvelo con cualquier servidor estático, por ejemplo
`npx serve web`. Necesita conexión para cargar Three.js desde cdnjs.

## Base para Unity (alternativa)

- Arquitectura, optimización y roadmap: [`docs/ARQUITECTURA.md`](docs/ARQUITECTURA.md)
- Scripts de C#: copia `Assets/_Project` dentro de un proyecto de Unity 6 (URP).
