# MISIÓN 01 - EMERGENCIA ENERGÉTICA

## Identificación

MissionId: mission_energy
MissionName: Emergencia energética

## Descripción

El astronauta despierta dentro de la estación
espacial cuando comienza a sonar una alarma.

El sistema informa que se ha detectado una falla
en el suministro energético.

El jugador debe seguir un protocolo de emergencia
para restablecer la energía y estabilizar
temporalmente los sistemas de la estación.

## Secuencia de objetivos

### Objetivo 1

ObjectiveId: energy_01

Description:
Activar el protocolo de emergencia
desde la consola principal.

RequiredAction:
EmergencyActivated

### Objetivo 2

ObjectiveId: energy_02

Description:
Localizar y recoger el módulo energético
de reemplazo.

RequiredAction:
EnergyModuleCollected

### Objetivo 3

ObjectiveId: energy_03

Description:
Instalar el módulo energético en el
sistema de ingeniería.

RequiredAction:
EnergyModuleInstalled

### Objetivo 4

ObjectiveId: energy_04

Description:
Verificar el suministro energético y
confirmar la reparación en la consola.

RequiredAction:
RepairConfirmed

## Condición de finalización

La misión termina cuando los cuatro objetivos
son completados en el orden establecido.

Si existen otras misiones configuradas,
MissionManager inicia automáticamente
la siguiente misión.

Si es la última misión configurada,
se completa la secuencia del videojuego.

## Integración con el equipo

Luis:
Gestiona la secuencia mediante MissionManager.

Santi:
Implementa el desplazamiento y los controles VR.

Yoshua:
Conecta los objetos interactivos mediante
MissionActionBridge y sus acciones.

Diogo:
Prepara las zonas de mando, laboratorio
e ingeniería dentro del escenario 3D.

Camila:
Muestra las instrucciones utilizando
CurrentObjective y OnObjectiveChanged.

## Configuración pendiente en Unity

Crear el ScriptableObject:

Game/Data/Missions/Mission_Energy.asset

Asignar:
- MissionId: mission_energy
- MissionName: Emergencia energética
- Objectives: 4 elementos

Configurar cada objetivo respetando
exactamente el orden indicado.

Agregar Mission_Energy.asset a la lista
de misiones de MissionManager.

Estado: pendiente de configuración y pruebas.