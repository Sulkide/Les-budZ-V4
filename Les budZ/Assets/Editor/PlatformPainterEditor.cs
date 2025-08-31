using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PlatformPainter))]
public class PlatformPainterEditor : Editor
{
    // Variables de gestion du drag
    private bool isDragging = false;
    private Vector3 dragStartPosition;
    private Vector3 currentDragPosition;
    private double dragStartTime = 0;
    private const float clickThreshold = 0.2f; // Seuil pour distinguer un clic court d'un drag

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
    }

    private void OnSceneGUI()
    {
        PlatformPainter painter = (PlatformPainter)target;
        Event e = Event.current;

        // Rafraîchir la vue pour les mouvements et le drag
        if(e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
            SceneView.RepaintAll();

        // Conversion de la position de la souris en coordonnées monde (2D : z = 0)
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        Vector3 mousePos = ray.origin;
        mousePos.z = 0;

        // Si l'utilisateur n'est pas en train de drag (ou pour le simple curseur), afficher la forme prévisualisée à la position de la souris
        if(!isDragging)
        {
            DrawPreviewShape(mousePos, painter.brushSize, painter);
        }
        // En mode non-line et pendant un drag, afficher la prévisualisation étirée
        else if(!painter.lineMode)
        {
            double dragDuration = EditorApplication.timeSinceStartup - dragStartTime;
            if(dragDuration >= clickThreshold)
            {
                DrawStretchedPreviewShape(dragStartPosition, currentDragPosition, painter);
            }
            else
            {
                // Pour un clic court, on affiche juste la forme normale au point de la souris
                DrawPreviewShape(mousePos, painter.brushSize, painter);
            }
        }
        // En mode line, toujours afficher la prévisualisation du curseur (la forme normale) au point de la souris
        else
        {
            DrawPreviewShape(mousePos, painter.brushSize, painter);
        }

        // Gestion des événements de la souris
        if(e.type == EventType.MouseDown && e.button == 0)
        {
            isDragging = true;
            dragStartPosition = mousePos;
            currentDragPosition = mousePos;
            dragStartTime = EditorApplication.timeSinceStartup;
            e.Use();
        }
        else if(e.type == EventType.MouseDrag && isDragging)
        {
            currentDragPosition = mousePos;
            e.Use();
        }
        else if(e.type == EventType.MouseUp && e.button == 0 && isDragging)
        {
            isDragging = false;
            currentDragPosition = mousePos;
            double dragDuration = EditorApplication.timeSinceStartup - dragStartTime;
            
            if(painter.lineMode)
            {
                if(dragDuration < clickThreshold)
                {
                    if(painter.showCircles)
                        CreateCircle(mousePos, painter.brushSize, painter);
                }
                else
                {
                    if(painter.showCircles)
                        CreateCircle(dragStartPosition, painter.brushSize, painter);

                    Vector3 delta = currentDragPosition - dragStartPosition;
                    Vector3 direction;
                    float projectedDistance;
                    if(painter.useFixedAngle)
                    {
                        float angle = 90f - painter.fixedAngle;
                        float angleRad = angle * Mathf.Deg2Rad;
                        direction = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0);
                        projectedDistance = Mathf.Max(0, Vector3.Dot(delta, direction));
                    }
                    else
                    {
                        direction = delta.normalized;
                        projectedDistance = delta.magnitude;
                    }
                    
                    if(projectedDistance <= 0)
                    {
                        if(painter.showCircles)
                            CreateCircle(dragStartPosition, painter.brushSize, painter);
                    }
                    else
                    {
                        CreateLine(dragStartPosition, projectedDistance, direction, painter.brushSize, painter);
                        if(painter.showCircles)
                        {
                            Vector3 secondCirclePos = dragStartPosition + direction * projectedDistance;
                            CreateCircle(secondCirclePos, painter.brushSize, painter);
                        }
                    }
                }
            }
            else
            {
                if(dragDuration < clickThreshold)
                {
                    CreatePlatformClick(mousePos, painter.brushSize, painter);
                }
                else
                {
                    CreatePlatform(dragStartPosition, currentDragPosition, painter);
                }
            }
            e.Use();
        }

        // En mode line, afficher la prévisualisation de la ligne pendant le drag (optionnel)
        if(isDragging && painter.lineMode && (EditorApplication.timeSinceStartup - dragStartTime) >= clickThreshold)
        {
            DrawLinePreview(dragStartPosition, currentDragPosition, painter.brushSize, painter);
        }

        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
    }

    // Fonction utilitaire pour dessiner la prévisualisation de la forme (non étirée) avec rotation
    private void DrawPreviewShape(Vector3 center, float brushSize, PlatformPainter painter)
    {
        Handles.color = Color.green;
        float effectiveRotation = painter.useFixedAngle ? 90f - painter.fixedAngle : painter.shapeRotation;
        switch (painter.shapeType)
        {
            case PlatformPainter.SpriteShape.Circle:
                Handles.DrawWireDisc(center, Vector3.forward, brushSize / 2);
                break;
            case PlatformPainter.SpriteShape.Square:
                {
                    Matrix4x4 m = Matrix4x4.TRS(center, Quaternion.Euler(0, 0, effectiveRotation), Vector3.one);
                    Matrix4x4 oldMat = Handles.matrix;
                    Handles.matrix = m;
                    Handles.DrawWireCube(Vector3.zero, new Vector3(brushSize, brushSize, 0));
                    Handles.matrix = oldMat;
                }
                break;
            case PlatformPainter.SpriteShape.Triangle:
                {
                    int numPoints = 3;
                    Vector3[] pts = new Vector3[numPoints + 1];
                    for (int i = 0; i < numPoints; i++)
                    {
                        float angle = 90f + i * 120f + effectiveRotation;
                        pts[i] = center + new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0) * (brushSize / 2);
                    }
                    pts[numPoints] = pts[0];
                    Handles.DrawPolyLine(pts);
                }
                break;
            case PlatformPainter.SpriteShape.Losange:
                {
                    Quaternion rot = Quaternion.Euler(0, 0, effectiveRotation);
                    Vector3 top = center + rot * Vector3.up * (brushSize / 2);
                    Vector3 right = center + rot * Vector3.right * (brushSize / 2);
                    Vector3 bottom = center + rot * Vector3.down * (brushSize / 2);
                    Vector3 left = center + rot * Vector3.left * (brushSize / 2);
                    Handles.DrawLine(top, right);
                    Handles.DrawLine(right, bottom);
                    Handles.DrawLine(bottom, left);
                    Handles.DrawLine(left, top);
                }
                break;
            case PlatformPainter.SpriteShape.Hexagon:
                {
                    int numPoints = 6;
                    Vector3[] pts = new Vector3[numPoints + 1];
                    for (int i = 0; i < numPoints; i++)
                    {
                        float angle = 90f + i * (360f / numPoints) + effectiveRotation;
                        pts[i] = center + new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0) * (brushSize / 2);
                    }
                    pts[numPoints] = pts[0];
                    Handles.DrawPolyLine(pts);
                }
                break;
        }
    }

    // Fonction pour dessiner la prévisualisation étirée de la forme lorsque l'utilisateur drag (mode non-line)
    private void DrawStretchedPreviewShape(Vector3 start, Vector3 current, PlatformPainter painter)
    {
        // Calcul du centre et de la taille (basé sur le drag)
        Vector3 center = (start + current) / 2;
        Vector3 size = new Vector3(Mathf.Abs(current.x - start.x), Mathf.Abs(current.y - start.y), 1);
        if(size.x < 0.001f || size.y < 0.001f)
        {
            DrawPreviewShape(current, painter.brushSize, painter);
            return;
        }
        // On calcule un facteur d'échelle basé sur la taille obtenue et le brushSize de base
        Vector3 scaleFactor = new Vector3(size.x / painter.brushSize, size.y / painter.brushSize, 1);
        // On définit une matrice de transformation qui applique la rotation shapeRotation et le facteur d'échelle
        Matrix4x4 m = Matrix4x4.TRS(center, Quaternion.Euler(0, 0, painter.shapeRotation), scaleFactor);
        
        switch(painter.shapeType)
        {
            case PlatformPainter.SpriteShape.Circle:
                {
                    int segments = 36;
                    Vector3[] pts = new Vector3[segments+1];
                    for(int i = 0; i <= segments; i++)
                    {
                        float angle = i * 360f/segments * Mathf.Deg2Rad;
                        Vector3 p = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * (painter.brushSize / 2);
                        pts[i] = m.MultiplyPoint(p);
                    }
                    Handles.DrawPolyLine(pts);
                }
                break;
            case PlatformPainter.SpriteShape.Square:
                {
                    Vector3[] pts = new Vector3[5];
                    pts[0] = m.MultiplyPoint(new Vector3(-painter.brushSize/2, painter.brushSize/2, 0));
                    pts[1] = m.MultiplyPoint(new Vector3(painter.brushSize/2, painter.brushSize/2, 0));
                    pts[2] = m.MultiplyPoint(new Vector3(painter.brushSize/2, -painter.brushSize/2, 0));
                    pts[3] = m.MultiplyPoint(new Vector3(-painter.brushSize/2, -painter.brushSize/2, 0));
                    pts[4] = pts[0];
                    Handles.DrawPolyLine(pts);
                }
                break;
            case PlatformPainter.SpriteShape.Triangle:
                {
                    Vector3[] pts = new Vector3[4];
                    for (int i = 0; i < 3; i++)
                    {
                        float angle = (90f + i * 120f) * Mathf.Deg2Rad;
                        pts[i] = m.MultiplyPoint(new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * (painter.brushSize/2));
                    }
                    pts[3] = pts[0];
                    Handles.DrawPolyLine(pts);
                }
                break;
            case PlatformPainter.SpriteShape.Losange:
                {
                    Vector3 top = m.MultiplyPoint(new Vector3(0, painter.brushSize/2, 0));
                    Vector3 right = m.MultiplyPoint(new Vector3(painter.brushSize/2, 0, 0));
                    Vector3 bottom = m.MultiplyPoint(new Vector3(0, -painter.brushSize/2, 0));
                    Vector3 left = m.MultiplyPoint(new Vector3(-painter.brushSize/2, 0, 0));
                    Handles.DrawLine(top, right);
                    Handles.DrawLine(right, bottom);
                    Handles.DrawLine(bottom, left);
                    Handles.DrawLine(left, top);
                }
                break;
            case PlatformPainter.SpriteShape.Hexagon:
                {
                    int segments = 6;
                    Vector3[] pts = new Vector3[segments+1];
                    for(int i = 0; i <= segments; i++)
                    {
                        float angle = (90f + i * 360f/segments) * Mathf.Deg2Rad;
                        pts[i] = m.MultiplyPoint(new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * (painter.brushSize/2));
                    }
                    Handles.DrawPolyLine(pts);
                }
                break;
        }
    }

    // Retourne le sprite correspondant à l'option choisie (pour la création définitive)
    private Sprite GetCircleSprite(PlatformPainter painter)
    {
        string spriteName = "";
        switch (painter.shapeType)
        {
            case PlatformPainter.SpriteShape.Circle:
                spriteName = "Circle";
                break;
            case PlatformPainter.SpriteShape.Square:
                spriteName = "Square";
                break;
            case PlatformPainter.SpriteShape.Triangle:
                spriteName = "Triangle";
                break;
            case PlatformPainter.SpriteShape.Losange:
                spriteName = "Losange";
                break;
            case PlatformPainter.SpriteShape.Hexagon:
                spriteName = "Hexagon";
                break;
            default:
                spriteName = "Circle";
                break;
        }
        return Resources.Load<Sprite>(spriteName);
    }

    // Création d'un objet de forme (similaire à CreateCircle) avec la rotation shapeRotation appliquée
    private void CreateCircle(Vector3 position, float brushSize, PlatformPainter painter)
    {
        GameObject circleObj = new GameObject("Circle");
        if (painter.parentObject != null)
        {
            circleObj.transform.SetParent(painter.parentObject.transform);
        }
        circleObj.transform.position = position;
        circleObj.transform.localScale = new Vector3(brushSize, brushSize, 1);
        // Calcul de la rotation effective
        float effectiveRotation = painter.useFixedAngle ? 90f - painter.fixedAngle : painter.shapeRotation;
        circleObj.transform.rotation = Quaternion.Euler(0, 0, effectiveRotation);

        SpriteRenderer sr = circleObj.AddComponent<SpriteRenderer>();
        Sprite shapeSprite = GetCircleSprite(painter);
        if (shapeSprite != null)
            sr.sprite = shapeSprite;
        else
            Debug.LogWarning("Le sprite '" + painter.shapeType.ToString() + "' n'a pas été trouvé dans Resources.");

        circleObj.AddComponent<PolygonCollider2D>();
        circleObj.layer = LayerMask.NameToLayer("Ground");
        Undo.RegisterCreatedObjectUndo(circleObj, "Create Circle");
    }


    // Création d'une ligne (rectangle) dont les extrémités correspondent aux centres des formes (en mode line)
    private void CreateLine(Vector3 start, float projectedDistance, Vector3 direction, float brushSize, PlatformPainter painter)
    {
        float rectLength = projectedDistance;
        if(rectLength <= 0) return;
        
        Vector3 rectCenter = start + direction * (rectLength / 2);

        GameObject lineObj = new GameObject("Line");
        if (painter.parentObject != null)
        {
            lineObj.transform.SetParent(painter.parentObject.transform);
        }

        lineObj.transform.position = rectCenter;
        float angleFinal = painter.useFixedAngle ? (90f - painter.fixedAngle) : Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        lineObj.transform.rotation = Quaternion.Euler(0, 0, angleFinal);
        lineObj.transform.localScale = new Vector3(rectLength, brushSize, 1);

        SpriteRenderer sr = lineObj.AddComponent<SpriteRenderer>();
        Sprite squareSprite = Resources.Load<Sprite>("Square");
        if(squareSprite != null)
            sr.sprite = squareSprite;
        else
            Debug.LogWarning("Le sprite 'Square' n'a pas été trouvé dans Resources.");

        lineObj.AddComponent<BoxCollider2D>();
        lineObj.layer = LayerMask.NameToLayer("Ground");
        Undo.RegisterCreatedObjectUndo(lineObj, "Create Line");
    }

    // Prévisualisation de la ligne en mode "lineMode"
    private void DrawLinePreview(Vector3 start, Vector3 end, float brushSize, PlatformPainter painter)
    {
        Handles.color = Color.green;
        if(painter.showCircles)
            DrawPreviewShape(start, brushSize, painter);

        Vector3 delta = end - start;
        Vector3 direction;
        float projectedDistance;
        if(painter.useFixedAngle)
        {
            float angle = 90f - painter.fixedAngle;
            float angleRad = angle * Mathf.Deg2Rad;
            direction = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0);
            projectedDistance = Mathf.Max(0, Vector3.Dot(delta, direction));
        }
        else
        {
            direction = delta.normalized;
            projectedDistance = delta.magnitude;
        }
        if(painter.showCircles)
        {
            Vector3 secondCirclePos = start + direction * projectedDistance;
            DrawPreviewShape(secondCirclePos, brushSize, painter);
        }

        float rectLength = projectedDistance;
        if(rectLength <= 0) return;
        Vector3 rectCenter = start + direction * (rectLength / 2);
        float angleFinal = painter.useFixedAngle ? (90f - painter.fixedAngle) : Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Matrix4x4 matrix = Matrix4x4.TRS(rectCenter, Quaternion.Euler(0, 0, angleFinal), Vector3.one);

        float halfLength = rectLength / 2;
        float halfHeight = brushSize / 2;
        Vector3 topLeft = new Vector3(-halfLength, halfHeight, 0);
        Vector3 topRight = new Vector3(halfLength, halfHeight, 0);
        Vector3 bottomRight = new Vector3(halfLength, -halfHeight, 0);
        Vector3 bottomLeft = new Vector3(-halfLength, -halfHeight, 0);

        Vector3 wpTopLeft = matrix.MultiplyPoint(topLeft);
        Vector3 wpTopRight = matrix.MultiplyPoint(topRight);
        Vector3 wpBottomRight = matrix.MultiplyPoint(bottomRight);
        Vector3 wpBottomLeft = matrix.MultiplyPoint(bottomLeft);

        Handles.DrawLine(wpTopLeft, wpTopRight);
        Handles.DrawLine(wpTopRight, wpBottomRight);
        Handles.DrawLine(wpBottomRight, wpBottomLeft);
        Handles.DrawLine(wpBottomLeft, wpTopLeft);
    }

    // Méthodes pour le mode plateforme classique (non-line)
    private void CreatePlatformClick(Vector3 position, float brushSize, PlatformPainter painter)
    {
        GameObject platform = new GameObject("Platform");
        if (painter.parentObject != null)
        {
            platform.transform.SetParent(painter.parentObject.transform);
        }

        platform.transform.position = position;
        platform.transform.localScale = new Vector3(brushSize, brushSize, 1);
        float effectiveRotation = painter.useFixedAngle ? 90f - painter.fixedAngle : painter.shapeRotation;
        platform.transform.rotation = Quaternion.Euler(0, 0, effectiveRotation);

        SpriteRenderer sr = platform.AddComponent<SpriteRenderer>();
        Sprite shapeSprite = GetCircleSprite(painter);
        if(shapeSprite != null)
            sr.sprite = shapeSprite;
        else
            Debug.LogWarning("Le sprite '" + painter.shapeType.ToString() + "' n'a pas été trouvé dans Resources.");

        platform.AddComponent<PolygonCollider2D>();
        platform.layer = LayerMask.NameToLayer("Ground");
        Undo.RegisterCreatedObjectUndo(platform, "Create Platform");
    }


    private void CreatePlatform(Vector3 start, Vector3 end, PlatformPainter painter)
    {
        Vector3 center = (start + end) / 2;
        Vector3 size = new Vector3(Mathf.Abs(end.x - start.x), Mathf.Abs(end.y - start.y), 1);
        GameObject platform = new GameObject("Platform");
        if (painter.parentObject != null)
        {
            platform.transform.SetParent(painter.parentObject.transform);
        }
        platform.transform.position = center;
        platform.transform.localScale = size;
        platform.transform.rotation = Quaternion.Euler(0, 0, painter.shapeRotation);

        SpriteRenderer sr = platform.AddComponent<SpriteRenderer>();
        Sprite shapeSprite = GetCircleSprite(painter);
        if(shapeSprite != null)
            sr.sprite = shapeSprite;
        else
            Debug.LogWarning("Le sprite '" + painter.shapeType.ToString() + "' n'a pas été trouvé dans Resources.");

        platform.AddComponent<PolygonCollider2D>();
        platform.layer = LayerMask.NameToLayer("Ground");
        Undo.RegisterCreatedObjectUndo(platform, "Create Platform");
    }
}
