# PLAN DE PRUEBAS E INTEGRACIÓN
## NASA SPACE APPS - Misión Espacial VR

Responsable: Luis - Sistema de misiones

## 1. Objetivo

Verificar que el sistema de misiones funcione
correctamente y pueda integrarse con los
sistemas VR, interacciones, interfaz y escenario.

## 2. Requisitos previos

- Unity debe compilar sin errores.
- La escena MissionVR.unity debe abrir correctamente.
- Debe existir un único MissionManager activo.
- Los scripts deben estar actualizados.
- MissionDebugTester debe tener asignado MissionManager.

## 3. Pruebas funcionales

### P01 - Inicio de misión
Acción: Ejecutar StartMission().
Esperado: Estado ActivateEmergency.
Evento: OnMissionStarted.

### P02 - Activación de emergencia
Acción: EmergencyActivated.
Esperado: Estado FindEnergyModule.

### P03 - Recoger módulo
Acción: EnergyModuleCollected.
Esperado: Estado RepairSystem.

### P04 - Instalación del módulo
Acción: EnergyModuleInstalled.
Esperado: Estado ConfirmRepair.

### P05 - Confirmación de reparación
Acción: RepairConfirmed.
Esperado: Estado Completed.
Evento: OnMissionCompleted.

### P06 - Acción fuera de orden
Acción: Instalar el módulo antes de recogerlo.
Esperado: La acción se rechaza.
El estado no cambia.

### P07 - Acción repetida
Acción: Activar emergencia dos veces.
Esperado: La segunda acción se rechaza.

### P08 - Reinicio
Acción: Ejecutar ResetMission().
Esperado: Estado NotStarted.
Evento: OnMissionReset.

### P09 - Volver a comenzar
Acción: Reiniciar y ejecutar StartMission().
Esperado: La misión comienza nuevamente.

### P10 - Gestor duplicado
Acción: Agregar un segundo MissionManager.
Esperado: El duplicado se detecta y desactiva.

## 4. Integración con el equipo

### Santi/Yoshua - Sistema VR
- Verificar movimiento y teleportación.
- Comprobar el funcionamiento de controladores.
- Permitir activar objetos interactivos.
- No modificar MissionManager.

### Santi/Yoshua - Interacciones
- Utilizar MissionActionBridge.
- Asignar MissionManager en el Inspector.
- Configurar MissionAction para cada objeto.
- Ejecutar ExecuteAction() al interactuar.
- Comprobar el valor devuelto por las acciones.
- Restaurar objetos al recibir OnMissionReset.

### Diogo - Escenario 3D
- Mantener colisiones y espacios navegables.
- Ubicar las consolas y objetos interactivos.
- Conservar los componentes al cambiar modelos.
- Coordinar modificaciones de MissionVR.unity.

### Camila - Interfaz
- Mostrar CurrentObjective al iniciar la interfaz.
- Escuchar OnObjectiveChanged.
- Mostrar victoria al recibir OnMissionCompleted.
- Conectar botones de inicio y reinicio.
- Actualizar la interfaz después de ResetMission.

## 5. Reglas de integración

- No modificar los estados directamente.
- Comunicar acciones mediante TryCompleteAction().
- No duplicar MissionManager.
- No depender de nombres específicos de modelos 3D.
- Suscribirse y desuscribirse de eventos correctamente.
- No editar simultáneamente MissionVR.unity.
- Mantener los archivos .meta de Unity en Git.
- Integrar y probar cada funcionalidad por separado.

## 6. Registro de resultados

Para cada prueba registrar:

- Código de prueba.
- Fecha.
- Integrante responsable.
- Resultado: Pendiente / Aprobado / Fallido.
- Error encontrado.
- Corrección aplicada.

## 7. Estado actual

Código del sistema de misiones: preparado.
Documentación de integración: preparada.
Compilación en Unity: pendiente.
Pruebas funcionales: pendientes.
Integración VR: pendiente.
Validación final: pendiente.