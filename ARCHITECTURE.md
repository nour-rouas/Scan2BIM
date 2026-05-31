# 🎨 ARQUITECTURA Y FLUJO VISUAL

## 📐 Arquitectura del Sistema

```
┌─────────────────────────────────────────────────────────────────┐
│                          REVIT 2024                             │
│                                                                 │
│  ┌───────────────────────────────────────────────────────┐    │
│  │                    REVIT UI                           │    │
│  │                                                       │    │
│  │  ┌─────────────────────────────────────────────┐    │    │
│  │  │        Add-Ins Tab                          │    │    │
│  │  │                                             │    │    │
│  │  │   ┌──────────────────────────────────┐    │    │    │
│  │  │   │     Floor Tools Panel            │    │    │    │
│  │  │   │                                  │    │    │    │
│  │  │   │  ┌─────────┐   ┌─────────┐     │    │    │    │
│  │  │   │  │Generar  │   │Deformar │     │    │    │    │
│  │  │   │  │ Puntos  │   │  Suelo  │     │    │    │    │
│  │  │   │  └────┬────┘   └────┬────┘     │    │    │    │
│  │  │   └───────┼─────────────┼──────────┘    │    │    │
│  │  └───────────┼─────────────┼───────────────┘    │    │
│  └──────────────┼─────────────┼────────────────────┘    │
│                 │             │                          │
│                 ▼             ▼                          │
│  ┌──────────────────────────────────────────────────┐  │
│  │     FloorToPointCloud Plugin DLL                 │  │
│  │                                                   │  │
│  │  ┌─────────────────────────────────────────┐   │  │
│  │  │             App.cs                      │   │  │
│  │  │      (IExternalApplication)             │   │  │
│  │  │  - OnStartup()                          │   │  │
│  │  │  - OnShutdown()                         │   │  │
│  │  │  - CreateRibbonPanel()                  │   │  │
│  │  └──────────────┬──────────────────────────┘   │  │
│  │                 │                               │  │
│  │       ┌─────────┴──────────┐                   │  │
│  │       ▼                    ▼                   │  │
│  │  ┌─────────┐          ┌─────────┐             │  │
│  │  │Command 1│          │Command 2│             │  │
│  │  │         │          │         │             │  │
│  │  └────┬────┘          └────┬────┘             │  │
│  │       │                    │                   │  │
│  └───────┼────────────────────┼───────────────────┘  │
│          │                    │                       │
│          ▼                    ▼                       │
│  ┌───────────────────────────────────────────────┐  │
│  │         REVIT DOCUMENT                        │  │
│  │                                               │  │
│  │  ┌─────────┐  ┌─────────┐  ┌─────────┐     │  │
│  │  │  Floor  │  │  Point  │  │ Family  │     │  │
│  │  │Elements │  │  Cloud  │  │Instance │     │  │
│  │  └─────────┘  └─────────┘  └─────────┘     │  │
│  └───────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────┘
```

---

## 🔄 Flujo del Comando 1: Generar Puntos

```
START
  │
  ├─► [1] Usuario click "Generar Puntos"
  │
  ├─► [2] Seleccionar Floor
  │      │
  │      ├─ Mostrar filtro (solo Floors)
  │      ├─ Usuario selecciona Floor
  │      └─ Validar selección
  │
  ├─► [3] Obtener Nube de Puntos
  │      │
  │      ├─ Buscar PointCloudInstance en documento
  │      ├─ Validar que existe
  │      └─ Obtener acceso a puntos
  │
  ├─► [4] Generar Grid de Puntos sobre Floor
  │      │
  │      ├─ Obtener geometría del Floor
  │      ├─ Identificar cara superior
  │      ├─ Calcular BoundingBox
  │      ├─ Generar malla regular (spacing: 0.5m)
  │      └─ Filtrar puntos dentro del contorno
  │
  ├─► [5] Calcular Elevaciones desde Nube
  │      │
  │      ├─ Para cada punto del grid:
  │      │   │
  │      │   ├─ Crear rayo vertical hacia arriba
  │      │   ├─ Buscar intersección con nube
  │      │   ├─ Obtener elevación del punto más cercano
  │      │   └─ Calcular diferencia (Δz)
  │      │
  │      └─ Resultado: Lista de ElevationPoints
  │
  ├─► [6] Crear Family Instances en Revit
  │      │
  │      ├─ Iniciar Transaction
  │      ├─ Obtener Family Symbol (punto genérico)
  │      ├─ Para cada ElevationPoint:
  │      │   │
  │      │   ├─ Crear FamilyInstance en posición
  │      │   └─ Asignar parámetros:
  │      │       - Elevación Base
  │      │       - Elevación Real
  │      │       - Diferencia
  │      │
  │      ├─ Agrupar todos los puntos creados
  │      └─ Commit Transaction
  │
  └─► [7] Mostrar mensaje de éxito
         │
         └─ "Se generaron N puntos de elevación"

END
```

---

## 🔄 Flujo del Comando 2: Deformar Floor

```
START
  │
  ├─► [1] Usuario click "Deformar Suelo"
  │
  ├─► [2] Seleccionar Floor
  │      │
  │      ├─ Mostrar filtro (solo Floors)
  │      ├─ Usuario selecciona Floor
  │      └─ Validar selección
  │
  ├─► [3] Detectar Puntos de Elevación
  │      │
  │      ├─ Obtener BoundingBox del Floor
  │      ├─ Buscar FamilyInstances cercanos
  │      ├─ Filtrar solo puntos de elevación
  │      └─ Validar cantidad mínima (>= 3)
  │
  ├─► [4] Extraer Datos de Elevación
  │      │
  │      ├─ Para cada punto:
  │      │   │
  │      │   ├─ Leer posición (XYZ)
  │      │   ├─ Parsear elevación real
  │      │   └─ Crear ElevationData
  │      │
  │      └─ Resultado: Lista de ElevationData
  │
  ├─► [5] Deformar Floor
  │      │
  │      ├─ Iniciar Transaction
  │      │
  │      ├─ Obtener SlabShapeEditor
  │      ├─ Validar que Floor es editable
  │      ├─ Habilitar shape editing
  │      │
  │      ├─ Para cada ElevationData:
  │      │   │
  │      │   ├─ Buscar/crear vértice en posición
  │      │   └─ Asignar elevación al vértice
  │      │
  │      ├─ (Opcional) Aplicar smoothing
  │      │
  │      └─ Commit Transaction
  │
  └─► [6] Mostrar mensaje de éxito
         │
         └─ "Floor deformado usando N puntos"

END
```

---

## 📦 Estructura de Clases

```
┌─────────────────────────────────────────────────────────────┐
│                    FloorToPointCloud                        │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐ │
│  │                     App.cs                           │ │
│  │                                                      │ │
│  │  + OnStartup(UIControlledApplication)               │ │
│  │  + OnShutdown(UIControlledApplication)              │ │
│  │  - CreateRibbonPanel()                              │ │
│  └──────────────────────────────────────────────────────┘ │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐ │
│  │                   Commands/                          │ │
│  │                                                      │ │
│  │  ┌────────────────────────────────────────────────┐ │ │
│  │  │  GenerateElevationPointsCommand                │ │ │
│  │  │                                                │ │ │
│  │  │  + Execute()                                   │ │ │
│  │  │  - SelectFloor()                               │ │ │
│  │  │  - GetPointCloud()                             │ │ │
│  │  │  - GenerateGridPoints()                        │ │ │
│  │  │  - CalculateElevations()                       │ │ │
│  │  │  - CreateElevationPointsInRevit()              │ │ │
│  │  └────────────────────────────────────────────────┘ │ │
│  │                                                      │ │
│  │  ┌────────────────────────────────────────────────┐ │ │
│  │  │  DeformFloorCommand                            │ │ │
│  │  │                                                │ │ │
│  │  │  + Execute()                                   │ │ │
│  │  │  - SelectFloor()                               │ │ │
│  │  │  - GetElevationPoints()                        │ │ │
│  │  │  - ExtractElevationData()                      │ │ │
│  │  │  - DeformFloor()                               │ │ │
│  │  └────────────────────────────────────────────────┘ │ │
│  └──────────────────────────────────────────────────────┘ │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐ │
│  │                      Core/                           │ │
│  │                                                      │ │
│  │  ┌────────────────────────────────────────────────┐ │ │
│  │  │  PointCloudAnalyzer                            │ │ │
│  │  │                                                │ │ │
│  │  │  + GetElevationAtPoint()                       │ │ │
│  │  │  - CreateSearchBox()                           │ │ │
│  │  │  - FindClosestPoint()                          │ │ │
│  │  └────────────────────────────────────────────────┘ │ │
│  │                                                      │ │
│  │  ┌────────────────────────────────────────────────┐ │ │
│  │  │  FloorGeometryManager                          │ │ │
│  │  │                                                │ │ │
│  │  │  + DeformFloorToElevations()                   │ │ │
│  │  │  - EnableShapeEditing()                        │ │ │
│  │  │  - ApplyElevations()                           │ │ │
│  │  │  - FindOrCreateVertex()                        │ │ │
│  │  └────────────────────────────────────────────────┘ │ │
│  │                                                      │ │
│  │  ┌────────────────────────────────────────────────┐ │ │
│  │  │  ElevationPointGenerator                       │ │ │
│  │  │                                                │ │ │
│  │  │  + CreatePointsInRevit()                       │ │ │
│  │  │  - GetPointSymbol()                            │ │ │
│  │  │  - SetParameters()                             │ │ │
│  │  └────────────────────────────────────────────────┘ │ │
│  └──────────────────────────────────────────────────────┘ │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐ │
│  │                     Utils/                           │ │
│  │                                                      │ │
│  │  ┌────────────────────────────────────────────────┐ │ │
│  │  │  GeometryUtils                                 │ │ │
│  │  │                                                │ │ │
│  │  │  + GenerateGridOnFloor()                       │ │ │
│  │  │  + IsPointInsidePolygon()                      │ │ │
│  │  │  - GenerateGridOnFace()                        │ │ │
│  │  └────────────────────────────────────────────────┘ │ │
│  │                                                      │ │
│  │  ┌────────────────────────────────────────────────┐ │ │
│  │  │  TransactionUtils                              │ │ │
│  │  │                                                │ │ │
│  │  │  + ExecuteInTransaction<T>()                   │ │ │
│  │  │  + ExecuteInTransaction()                      │ │ │
│  │  └────────────────────────────────────────────────┘ │ │
│  │                                                      │ │
│  │  ┌────────────────────────────────────────────────┐ │ │
│  │  │  Logger                                        │ │ │
│  │  │                                                │ │ │
│  │  │  + Log(string)                                 │ │ │
│  │  │  + LogError(Exception)                         │ │ │
│  │  └────────────────────────────────────────────────┘ │ │
│  └──────────────────────────────────────────────────────┘ │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 🔗 Data Flow - Comando 1

```
  USER ACTION                 PROCESSING                  DATA CREATED
       │                           │                            │
       │                           │                            │
 ┌─────▼──────┐            ┌──────▼───────┐           ┌───────▼────────┐
 │   Click    │            │    Select    │           │                │
 │  "Generar  │───────────►│    Floor     │──────────►│  Floor Object  │
 │   Puntos"  │            │   Element    │           │                │
 └────────────┘            └──────────────┘           └───────┬────────┘
                                                               │
                                                               │
                           ┌──────────────┐           ┌───────▼────────┐
                           │    Get       │           │                │
                           │  PointCloud  │──────────►│ PointCloud Obj │
                           │   Instance   │           │                │
                           └──────────────┘           └───────┬────────┘
                                                               │
                                                               │
                           ┌──────────────┐           ┌───────▼────────┐
                           │   Generate   │           │                │
                           │  Grid Points │──────────►│  List<XYZ>     │
                           │  (0.5m grid) │           │  Grid Points   │
                           └──────────────┘           └───────┬────────┘
                                                               │
                                                               │
                                    ┌──────────────────────────┘
                                    │
                           ┌────────▼─────┐           ┌─────────────────┐
                           │  Calculate   │           │                 │
                           │  Elevation   │──────────►│ List<Elevation  │
                           │  from Cloud  │           │    Point>       │
                           └──────────────┘           └────────┬────────┘
                                                                │
                                                                │
                           ┌──────────────┐           ┌────────▼────────┐
                           │    Create    │           │                 │
                           │   Family     │──────────►│ FamilyInstance  │
                           │  Instances   │           │   Elements      │
                           └──────────────┘           │   (in Revit)    │
                                                      └─────────────────┘
```

---

## 🔗 Data Flow - Comando 2

```
  USER ACTION                 PROCESSING                  RESULT
       │                           │                         │
       │                           │                         │
 ┌─────▼──────┐            ┌──────▼───────┐          ┌─────▼──────┐
 │   Click    │            │    Select    │          │            │
 │ "Deformar  │───────────►│    Floor     │─────────►│Floor Object│
 │   Suelo"   │            │   Element    │          │            │
 └────────────┘            └──────────────┘          └─────┬──────┘
                                                            │
                                                            │
                           ┌──────────────┐          ┌─────▼──────┐
                           │     Find     │          │            │
                           │  Elevation   │─────────►│ List of    │
                           │    Points    │          │ Points     │
                           └──────────────┘          └─────┬──────┘
                                                            │
                                                            │
                           ┌──────────────┐          ┌─────▼──────┐
                           │   Extract    │          │            │
                           │  Elevation   │─────────►│List<Elev   │
                           │     Data     │          │   Data>    │
                           └──────────────┘          └─────┬──────┘
                                                            │
                                                            │
                           ┌──────────────┐                │
                           │    Enable    │◄───────────────┘
                           │    Shape     │
                           │   Editing    │
                           └──────┬───────┘
                                  │
                           ┌──────▼───────┐          ┌─────────────┐
                           │   Create     │          │             │
                           │  Vertices &  │─────────►│  Deformed   │
                           │   Assign     │          │    Floor    │
                           │  Elevations  │          │             │
                           └──────────────┘          └─────────────┘
```

---

## 🎯 Estados del Floor

```
 INICIAL                  INTERMEDIO                    FINAL
┌────────┐              ┌────────────┐              ┌────────────┐
│        │              │    ●  ●    │              │   ╱╲  ╱╲   │
│ Floor  │   Cmd 1     │  ●  ●  ●   │   Cmd 2     │  ╱  ╲╱  ╲  │
│  Plano │  ────────►  │ ●  ●  ●  ● │  ────────►  │ ╱  Floor  ╲ │
│        │              │  ●  ●  ●   │              │╱ Deformado ╲│
└────────┘              │    ●  ●    │              └────────────┘
                        └────────────┘
                        Puntos con
                         elevaciones
```

---

## 📊 Diagrama de Dependencias

```
                           ┌──────────┐
                           │   User   │
                           └────┬─────┘
                                │
                        ┌───────▼────────┐
                        │   Revit UI     │
                        │   (Ribbon)     │
                        └───────┬────────┘
                                │
                        ┌───────▼────────┐
                        │    App.cs      │
                        └───────┬────────┘
                                │
                    ┌───────────┴───────────┐
                    │                       │
            ┌───────▼────────┐      ┌──────▼───────┐
            │   Command 1    │      │  Command 2   │
            └───────┬────────┘      └──────┬───────┘
                    │                      │
        ┌───────────┼──────────┐          │
        │           │          │          │
  ┌─────▼─────┐ ┌──▼──────┐ ┌─▼─────────▼┐
  │Geometry   │ │  Point  │ │   Floor    │
  │  Utils    │ │  Cloud  │ │  Geometry  │
  │           │ │Analyzer │ │  Manager   │
  └───────────┘ └─────────┘ └────────────┘
        │           │              │
        └───────────┼──────────────┘
                    │
            ┌───────▼────────┐
            │  Transaction   │
            │     Utils      │
            └────────────────┘
                    │
            ┌───────▼────────┐
            │     Logger     │
            └────────────────┘
```

---

## 🔄 Ciclo de Vida de una Transaction

```
    START
      │
      ▼
┌─────────────┐
│   using     │
│ Transaction │
└──────┬──────┘
       │
       ▼
┌─────────────┐
│trans.Start()│
└──────┬──────┘
       │
       ▼
┌─────────────┐
│  Modificar  │
│   Modelo    │
│  (Revit)    │
└──────┬──────┘
       │
       ▼
   ┌───────┐
   │¿Error?│
   └───┬───┘
       │
   ┌───┴───┐
   │       │
  YES     NO
   │       │
   ▼       ▼
┌─────┐ ┌──────┐
│Roll │ │Commit│
│Back │ │      │
└──┬──┘ └───┬──┘
   │        │
   └────┬───┘
        │
        ▼
      END
```

---

## 💾 Modelo de Datos

### ElevationPoint
```
┌─────────────────────────┐
│    ElevationPoint       │
├─────────────────────────┤
│ + Position: XYZ         │ ◄── Coordenadas 3D
│ + BaseElevation: double │ ◄── Elevación del floor plano
│ + RealElevation: double │ ◄── Elevación real de la nube
│ + Difference: double    │ ◄── ΔZ = Real - Base
└─────────────────────────┘
```

### ElevationData
```
┌─────────────────────────┐
│     ElevationData       │
├─────────────────────────┤
│ + Position: XYZ         │ ◄── Coordenadas 3D
│ + Elevation: double     │ ◄── Elevación a aplicar
└─────────────────────────┘
```

---

## 🎨 Visualización de Proceso

```
┌─────────────────────────────────────────────────────────────┐
│                    WORKFLOW VISUAL                          │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  DAY 1-2         DAY 3          DAY 4-7        DAY 8-11    │
│    │               │                │              │        │
│    ▼               ▼                ▼              ▼        │
│  ┌───┐          ┌────┐          ┌─────┐       ┌──────┐    │
│  │Set│          │ UI │          │Cmd 1│       │Cmd 2 │    │
│  │up │  ───►    │    │  ───►    │     │  ───► │      │    │
│  └───┘          └────┘          └─────┘       └──────┘    │
│    ✓               ✓                ✓              ✓       │
│                                                             │
│                         DAY 12-14                           │
│                            │                                │
│                            ▼                                │
│                        ┌────────┐                           │
│                        │Testing │                           │
│                        │  & Doc │                           │
│                        └────────┘                           │
│                            ✓                                │
│                                                             │
│                       🎉 COMPLETE 🎉                        │
└─────────────────────────────────────────────────────────────┘
```

---

**¡Usa estos diagramas como referencia visual durante el desarrollo! 🎨**
