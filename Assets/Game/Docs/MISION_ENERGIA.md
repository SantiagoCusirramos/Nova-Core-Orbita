# MISIÓN 01 - SUSTITUCIÓN DEL MÓDULO ENERGÉTICO

## 1. Identificación

MissionId: mission_energy
MissionName: Contingencia energética

## 2. Descripción

El astronauta despierta debido a una alarma
de emergencia en la estación espacial.

El Centro de Control informa de una falla
en el sistema de distribución eléctrica.

El jugador deberá localizar un módulo de
reemplazo y utilizar un brazo robótico para
sustituir el componente averiado.

## 3. Objetivos de Core

### Objetivo 1
ObjectiveId: energy_01
Description: Activar el protocolo de emergencia.
RequiredAction: EmergencyActivated

### Objetivo 2
ObjectiveId: energy_02
Description: Localizar y recoger el módulo energético.
RequiredAction: EnergyModuleCollected

### Objetivo 3
ObjectiveId: energy_03
Description: Introducir el repuesto en la compuerta.
RequiredAction: EnergyModuleLoadedIntoHatch

### Objetivo 4
ObjectiveId: energy_04
Description: Activar la consola de control robótico.
RequiredAction: RoboticConsoleActivated

### Objetivo 5
ObjectiveId: energy_05
Description: Sustituir el módulo usando el brazo robótico.
RequiredAction: EnergyModuleInstalled

### Objetivo 6
ObjectiveId: energy_06
Description: Verificar y confirmar la reparación.
RequiredAction: RepairConfirmed

## 4. Minijuego de sustitución robótica

El minijuego comienza después de activar
la consola robótica.

Secuencia interna:

1. Identificar el módulo defectuoso.
2. Posicionar el brazo robótico.
3. Sujetar y retirar el módulo averiado.
4. Depositarlo en una zona segura.
5. Recoger el módulo de reemplazo.
6. Introducirlo en su alojamiento.
7. Asegurar la instalación.
8. Ejecutar la comprobación de instalación.

Estas etapas son internas del minijuego.

Solo cuando se completen correctamente
se enviará a Core la acción:

EnergyModuleInstalled

Si un paso falla, el minijuego deberá permitir
corregirlo sin avanzar el objetivo de Core.

## 5. Responsabilidades

Luis:
- Definir los objetivos.
- Configurar MissionDefinition.
- Validar la integración con MissionManager.

Santi:
- Configurar controles VR.
- Implementar el modo de operación de consola.
- Mantener el seguimiento natural de la cabeza.

Yoshua:
- Programar la compuerta interactiva.
- Programar el brazo y su sistema de agarre.
- Implementar la secuencia del minijuego.
- Notificar su finalización a Core.

Diogo:
- Preparar módulo, compuerta y brazo 3D.
- Organizar la zona de mantenimiento.
- Configurar elementos visuales y colisiones.

Camila:
- Diseñar interfaz de operación robótica.
- Mostrar objetivos y pasos del minijuego.
- Preparar instrucciones del Centro de Control.
- Mostrar errores y confirmaciones.

## 6. Integración con Core

MissionManager valida los seis objetivos
según su orden.

MissionActionBridge comunica las acciones.

El minijuego mantiene sus estados internos
separados de MissionManager.

El minijuego no debe enviar
EnergyModuleInstalled hasta terminar
todas las comprobaciones.

Las acciones solo deben enviarse después
de validar la interacción correspondiente.

## 7. Finalización

Tras confirmar la reparación, la misión termina.

Si existe una siguiente misión configurada,
Core avanzará automáticamente.

La siguiente misión prevista es la
reparación exterior del panel solar.

## 8. Estado actual

Diseño de misión: definido.
Acciones Core: declaradas.
Configuración Mission_Energy.asset: pendiente.
Minijuego robótico: pendiente de implementación.
Pruebas de integración: pendientes.