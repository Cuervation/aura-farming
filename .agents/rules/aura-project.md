# Aura Farming - Project Rules & Development Guidelines

## 1. Project Overview & Tech Stack
- **Project Name:** Aura Farming
- **Engine:** Unity 6 (URP - Universal Render Pipeline)
- **Language:** C#
- **Methodology:** Spec-Driven Development (SDD)

---

## 2. Spec-Driven Development (SDD) & Quality Gates
- **No Spec, No Code:** Ninguna feature se implementa sin una especificación formal y criterios de aceptación previamente definidos y acordados.
- **Source of Truth:** Toda implementación debe alinearse con la documentación oficial del proyecto.
- **Separation of Concerns:** El agente o desarrollador que implementa una feature no debe ser el único responsable de aprobarla (requiere verificación o revisión cruzada).
- **Pre-Completion Checks:** Antes de dar por finalizada cualquier tarea, es obligatorio verificar:
  - Cero errores o advertencias críticas de compilación.
  - Ausencia de `NullReferenceException` o referencias perdidas (`MissingReferenceException` / missing component references).
  - Cumplimiento estricto de todos los criterios de aceptación especificados.

---

## 3. Conflict Resolution & Product Decisions
- **Conflict Management:** Si una especificación, la arquitectura existente y una solicitud de implementación entran en conflicto, se debe detener la implementación de inmediato y señalar el conflicto para clarificación.
- **No Unspecified Decisions:** No inventar requisitos ni tomar decisiones de producto importantes sin contar con una especificación previa.

---

## 4. Architecture & Coding Standards
- **Simplicity First:** Mantener la solución simple (KISS/YAGNI) y evitar sobrearquitectura o abstracciones innecesarias.
- **Composition over Inheritance:** Priorizar composición y componentes modulares sobre jerarquías complejas de herencia.
- **Decoupled Gameplay & UI:** La lógica de gameplay debe mantenerse estrictamente desacoplada de la interfaz de usuario (UI) y la capa de presentación.
- **Data-Driven Design:** Evitar valores "hardcodeados" en código; utilizar `ScriptableObject`s o configuración serializada cuando corresponda.
- **Architecture Documentation:** Todas las decisiones arquitectónicas significativas deben quedar documentadas.
- **Scope Discipline:** No modificar archivos ajenos a la tarea en curso sin una justificación clara.
- **External Dependencies:** No introducir paquetes externos, librerías de terceros o dependencias de Package Manager sin justificación fundamentada.
- **Language Conventions:**
  - **Código:** Todo el código (nombres de clases, interfaces, structs, métodos, variables, namespaces, enums y comentarios técnicos) debe escribirse en **inglés**.
  - **Documentación y Comunicación:** La documentación, especificaciones, conversaciones y revisiones pueden redactarse en **español**.

---

## 5. Unity Asset & Metadata Safety
- **Meta File Integrity:** Nunca eliminar, regenerar o modificar archivos `.meta` salvo que sea consecuencia necesaria de crear, mover o eliminar deliberadamente un asset.
- **Preserve GUIDs:** Preservar estrictamente los GUIDs de Unity para evitar roturas de referencias.
- **Protected Core Directories:** No modificar `ProjectSettings/` ni `Packages/` salvo que la tarea o especificación lo requiera explícitamente.
- **Scope-Limited Serialized Assets:** Evitar modificar escenas, prefabs o assets serializados que no pertenezcan al alcance directo de la tarea.

---

## 6. Testing & Quality Assurance
- **Unit & Logic Testing:** Toda lógica desacoplada de `MonoBehaviour` que sea razonablemente testeable debe incluir o actualizar tests cuando corresponda.
- **Regression Tests:** Los bugs corregidos deberían incluir un test de regresión cuando sea viable.

---

## 7. Security & Secrets Management
- **Zero Secrets in Repo:** Nunca almacenar API keys, tokens, passwords, secrets o credenciales dentro del repositorio.

---

## 8. Git Safety & Workflow Guidelines
- **Protected Main Branches:** Queda prohibido modificar o commitear directamente sobre las ramas `main` o `master`.
- **Isolated Workspaces:** Cada feature, corrección o tarea debe desarrollarse en su propia rama o worktree aislado.
- **No Unauthorized Git Actions:** No ejecutar commit, push, merge, rebase, reset, force push ni operaciones destructivas de Git salvo autorización explícita de la tarea.
- **Modification Transparency:** Antes de finalizar cualquier tarea, informar detalladamente todos los archivos que fueron modificados.

---

## 9. Source of Truth
Cuando existan en el repositorio, se deben consultar obligatoriamente los siguientes documentos como fuente de verdad:
- `Docs/00_GAME_VISION.md`
- `Docs/01_GAME_DESIGN_SPEC.md`
- `Docs/02_TECHNICAL_SPEC.md`
- `Docs/03_ARCHITECTURE.md`
- `Docs/Specs/` (especificaciones individuales por feature)
