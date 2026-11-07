# MISIÓN 02 - REPARACIÓN DEL PANEL SOLAR

## 1. Identificación

MissionId: mission_solar
MissionName: Reparación exterior del panel solar

## 2. Descripción

Después de reemplazar el módulo energético,
el Centro de Control detecta que la estación
todavía presenta una falla de generación eléctrica.

Uno de los paneles solares está dañado.

El astronauta deberá prepararse para una
actividad extravehicular (EVA), salir de la
estación y desplazarse mediante una unidad
de maniobra personal inspirada en el MMU.

Al llegar al panel, realizará una reparación
guiada y regresará a la estación.

## 3. Objetivos de Core

### Objetivo 1
ObjectiveId: solar_01
Description: Equiparse con el traje espacial.
RequiredAction: SuitEquipped

### Objetivo 2
ObjectiveId: solar_02
Description: Verificar los sistemas del traje.
RequiredAction: SuitSystemsChecked

### Objetivo 3
ObjectiveId: solar_03
Description: Completar la preparación EVA.
RequiredAction: EVAPreparationCompleted

### Objetivo 4
ObjectiveId: solar_04
Description: Despresurizar la esclusa.
RequiredAction: AirlockDepressurized

### Objetivo 5
ObjectiveId: solar_05
Description: Activar el sistema de propulsión personal.
RequiredAction: MMUActivated

### Objetivo 6
ObjectiveId: solar_06
Description: Llegar y estabilizarse junto al panel solar.
RequiredAction: SolarPanelReached

### Objetivo 7
ObjectiveId: solar_07
Description: Reparar el panel solar dañado.
RequiredAction: SolarPanelRepaired

### Objetivo 8
ObjectiveId: solar_08
Description: Regresar a la esclusa de la estación.
RequiredAction: ReturnedToAirlock

### Objetivo 9
ObjectiveId: solar_09
Description: Completar el retorno y represurización.
RequiredAction: AirlockRepressurized

### Objetivo 10
ObjectiveId: solar_10
Description: Confirmar la estabilización energética.
RequiredAction: StationPowerVerified

## 4. Minijuego de reparación exterior

Propuesta inicial:
Reparación de un panel rasgado con refuerzos.

Secuencia interna:

1. Inspeccionar visualmente el panel.
2. Identificar la zona dañada.
3. Seleccionar la herramienta indicada.
4. Liberar el elemento atascado.
5. Instalar tres refuerzos en puntos definidos.
6. Comprobar las fijaciones.
7. Solicitar una prueba de funcionamiento.

El Centro de Control proporcionará
instrucciones durante la reparación.

Al completar correctamente todos los pasos,
se enviará a Core:

SolarPanelRepaired

Si existe un error, el jugador deberá
corregirlo antes de continuar.

La secuencia es una simulación educativa
simplificada, no un procedimiento EVA real.

## 5. Responsabilidades

Luis:
- Definir y configurar los objetivos.
- Integrar el resultado del minijuego con Core.
- Verificar el avance de la misión.

Santi:
- Implementar movimiento y controles VR.
- Programar el desplazamiento con el MMU.
- Implementar estabilización cerca del panel.
- Mantener la comodidad del jugador en VR.

Yoshua:
- Programar las interacciones del traje.
- Implementar controles interactivos
  de la esclusa.
- Programar el minijuego del panel solar.
- Validar que la reparación esté completa.
- Enviar las acciones correspondientes a Core.

Diogo:
- Preparar el traje y la esclusa.
- Crear o adaptar el modelo del MMU.
- Preparar el entorno exterior.
- Crear panel dañado, refuerzos y herramientas.
- Configurar colisiones y puntos de interacción.

Camila:
- Diseñar mensajes e instrucciones EVA.
- Crear indicadores del traje y del MMU.
- Implementar la interfaz del minijuego.
- Preparar comunicaciones del Centro de Control.
- Mostrar confirmaciones y errores.

## 6. Integración con Core

La misión se configurará mediante:

MissionDefinition
MissionObjective
MissionAction

MissionManager gestionará los objetivos
en el orden establecido.

Los sistemas interactivos enviarán
las acciones mediante MissionActionBridge.

El minijuego enviará SolarPanelRepaired
únicamente cuando complete su secuencia.

La navegación del MMU será independiente
de MissionManager.

Al reiniciar la misión, los componentes
deberán restaurarse a su estado inicial.

## 7. Finalización

La misión terminará cuando el astronauta
regrese a la estación y confirme la
estabilización energética.

Si esta es la última misión configurada,
MissionManager emitirá OnMissionCompleted.

La interfaz mostrará la pantalla final.

## 8. Estado actual

Diseño de objetivos: propuesto.
Acciones Core: declaradas.
Minijuego definitivo: por confirmar.
Mission_SolarPanel.asset: pendiente.
Mecánicas EVA y MMU: pendientes.
Integración y pruebas: pendientes.