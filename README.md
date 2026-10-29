# Buenos Aires Colonial · 1810

Mapa antiguo interactivo de Buenos Aires en mayo de 1810 y un juego en primera persona hecho en Unity.

**Jugar:** https://cluduenaorellano014-afk.github.io/buenos-aires-colonial-1810/
**Juego 3D directo:** https://cluduenaorellano014-afk.github.io/buenos-aires-colonial-1810/unity/

- Mapa con la Plaza Mayor, el Cabildo, la Jabonería de Vieytes, la Casa de Don Blas, la Catedral, el Fuerte y cientos de casas coloniales.
- Tocá un pin o una casa para ver su descripción; "Teletransportarse" abre el juego de Unity en ese lugar y "Vista en 3D" lo abre en la Plaza Mayor.
- La ciudad 3D tiene las mismas manzanas y casas que el mapa, todo en estilo low poly.
- Interiores explorables, con animación de entrada (se abren las puertas y la cámara entra):
  - **Cabildo:** recova, zaguán, escribanía, cárcel, patio con aljibe, escalera, Sala Capitular y balcón sobre la plaza.
  - **Catedral:** naves con arcos, cúpula, retablo dorado y púlpito. La fachada está sin terminar, como en 1810.
  - **Real Fortaleza:** foso, puente levadizo, murallas con cañones, plaza de armas, cuartel y Salón del Virrey.
  - **Jabonería de Vieytes:** tienda, pailas hirviendo, moldes, corral y la trastienda donde se reúnen los patriotas.
  - **Casa de Don Blas:** zaguán, tienda, dos patios, sala de tertulia con estrado, comedor, cocina, parral y huerta.
- Vecinos, vendedores, soldados y frailes conversan sobre la Revolución de Mayo en globos de texto.

## Controles

| | Computadora | Celular / tablet |
|---|---|---|
| Caminar | W A S D o flechas | Arrastrar en la mitad izquierda (joystick) |
| Mirar | Mouse (clic para capturarlo) | Arrastrar en la mitad derecha |
| Correr | Shift | Botón "Correr" |
| Entrar / salir | E junto a una puerta | Botón "Entrar" / "Salir" |
| Viajar | Botón "Lugares" | Botón "Lugares" |
| Volver al mapa | M o botón | Botón |

## Cómo está hecho

- `buenos_aires_colonial.html`: el mapa (SVG). Si el juego de Unity no está disponible, usa un 3D propio hecho con Three.js.
- `unity/`: la versión web (WebGL 2) del juego de Unity 6 (URP). La ciudad se genera al iniciar a partir de los datos del mapa, así la descarga es chica.
- `unity-src/`: los scripts de Unity (C#) que generan la ciudad, los edificios, las personas y la interfaz.
