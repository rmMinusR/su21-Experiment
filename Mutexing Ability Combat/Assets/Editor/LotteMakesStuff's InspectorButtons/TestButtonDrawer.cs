// NOTE put in a Editor folder

using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections;
using System.Linq;

namespace Perception.Editor {

    [CustomPropertyDrawer(typeof(TestButtonAttribute))]
    public class TestButtonDrawer : DecoratorDrawer
    {
        public override void OnGUI(Rect position)
        {
            // cast the attribute to make it easier to work with
            TestButtonAttribute buttonAttribute = (attribute as TestButtonAttribute);

            // check if the button is supposed to be enabled right now
            if (EditorApplication.isPlaying && !buttonAttribute.isActiveAtRuntime)
                GUI.enabled = false;
            if (!EditorApplication.isPlaying && !buttonAttribute.isActiveInEditor)
                GUI.enabled = false;

            // figure out where were drawing the button
            Rect pos = new Rect(position.x, position.y, position.width, position.height - EditorGUIUtility.standardVerticalSpacing);
            // draw it and if its clicked...
            if (GUI.Button(pos, buttonAttribute.buttonLabel))
            {
                //Scan the selection for the requested method
                Object sel = Selection.activeObject;
                object methodOwner = null;
                MethodInfo method;

                method = sel.GetType().GetMethod(buttonAttribute.methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if(method != null) methodOwner = sel;

                if(method == null && sel is GameObject go)
                {
                    foreach(Component c in go.GetComponents<Component>())
                    {
                        method = c.GetType().GetMethod(buttonAttribute.methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if(method != null)
                        {
                            methodOwner = c;
                            goto tryExecMethod; //BAD PRACTICE: TODO find a better way
                        } 
                    }
                }

                //Execute requested method
                tryExecMethod:
                if (method != null)
                {
                    object ret = method.Invoke(methodOwner, new object[] { });
                    if (method.ReturnType == typeof(IEnumerator) && methodOwner is MonoBehaviour mo)
                    {
                        mo.StartCoroutine(ret as IEnumerator);
                    }
                }
            }

            // make sure the GUI is enabled when were done!
            GUI.enabled = true;
        }

        public override float GetHeight()
        {
            return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing*2;
        }
    }

}