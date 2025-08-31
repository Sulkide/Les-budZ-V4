namespace ImporterForGIMPImageFilesEditor {
    using UnityEditor;
    using UnityEngine;

    internal class VersionChanges : EditorWindow {

        //Variables.
        Vector2 scrollPosition = Vector2.zero;
        GUIStyle _headerLabel = null;
        GUIStyle headerLabel {
            get {
                if (_headerLabel == null) {
                    _headerLabel = new GUIStyle(EditorStyles.boldLabel);
                    _headerLabel.alignment = TextAnchor.MiddleCenter;
                    _headerLabel.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
                }
                return _headerLabel;
            }
        }
        GUIStyle _subHeaderLabel = null;
        GUIStyle subHeaderLabel {
            get {
                if (_subHeaderLabel == null) {
                    _subHeaderLabel = new GUIStyle(EditorStyles.boldLabel);
                    _subHeaderLabel.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
                }
                return _subHeaderLabel;
            }
        }
        GUIStyle _boldWrappedLabel = null;
        GUIStyle boldWrappedLabel {
            get {
                if (_boldWrappedLabel == null) {
                    _boldWrappedLabel = new GUIStyle(EditorStyles.boldLabel);
                    _boldWrappedLabel.wordWrap = true;
                }
                return _boldWrappedLabel;
            }
        }
        GUIStyle _wrappedLabel = null;
        GUIStyle wrappedLabel {
            get {
                if (_wrappedLabel == null) {
                    _wrappedLabel = new GUIStyle(EditorStyles.label);
                    _wrappedLabel.padding = new RectOffset(25, 0, 0, 0);
                    _wrappedLabel.wordWrap = true;
                }
                return _wrappedLabel;
            }
        }
        GUIStyle _bulletPointAlignedAtTop = null;
        GUIStyle bulletPointAlignedAtTop {
            get {
                if (_bulletPointAlignedAtTop == null) {
                    _bulletPointAlignedAtTop = new GUIStyle(EditorStyles.label);
                    _bulletPointAlignedAtTop.alignment = TextAnchor.UpperCenter;
                }
                return _bulletPointAlignedAtTop;
            }
        }

        //Draw the GUI.
        void OnGUI() {

            //Display the version change text.
            EditorGUILayout.LabelField("Importer for GIMP Image Files Version Changes", headerLabel);
            EditorGUILayout.GetControlRect();
            EditorGUILayout.LabelField("If you have any comments or suggestions as to how we could improve Importer for GIMP Image Files, or if you want to " +
                    "report a bug in the software, feel free to e-mail us on info@battenbergsoftware.com and we'll get back to you. Thanks!", boldWrappedLabel);
            EditorGUILayout.GetControlRect();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.LabelField("Version 1.0.0", subHeaderLabel);
            EditorGUILayout.LabelField("Initial release.", wrappedLabel);
            addBulletPoint();
            EditorGUILayout.EndScrollView();
        }

        //Adds a bullet point before the label that has just been added.
        void addBulletPoint() {
            Rect rect = GUILayoutUtility.GetLastRect();
            EditorGUI.LabelField(new Rect(17, rect.yMin - 1, 12, rect.height), "•", bulletPointAlignedAtTop);
        }
    }
}