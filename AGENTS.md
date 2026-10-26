# ¿Who is outside? — contexto para el agente

Videojuego 2D de terror (estilo That's Not My Neighbor).

## Stack

- Unity 6.5 (6000.5.10f1), URP, Input System nuevo
- UI: uGUI (Canvas) + TextMeshPro
- Código en Assets/\_Project (Scripts, Scenes, Sprites, Audio, Data, Prefabs)
- Datos con ScriptableObjects

## Reglas de diseño (no cambiar sin preguntar)

- Flujo: Rechazar cierra la ventanilla y bloquea la puerta; solo entonces se habilita Emergencia. Siempre es Rechazar y luego Emergencia.
- Dejar pasar a un impostor = derrota inmediata con screamer.
- Reportar a un residente real = pantalla de despedida por mal trabajo.
- Los documentos (credencial, etc.) son imágenes completas: NO superponer texto TMP, solo cambiar el sprite.
- Las credenciales y la solicitud se abren como modales arrastrables.

## Forma de trabajo

- Una tarea pequeña por vez; muestra el plan antes de editar.
- No modifiques escenas ni .meta sin avisar. Compila y revisa la consola al terminar.
