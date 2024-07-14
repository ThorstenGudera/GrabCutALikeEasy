using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Runtime.Serialization;

namespace AvoidAGrabCutEasy
{
    sealed class AvoidAGrabCut_To_Easy_Binder : SerializationBinder
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            if (assemblyName.Contains("AvoidAGrabCut") && !assemblyName.EndsWith("Easy"))
                assemblyName = Assembly.GetExecutingAssembly().FullName;

            if (typeName.Contains("AvoidAGrabCut") && !typeName.EndsWith("Easy"))
                typeName = typeName.Replace("AvoidAGrabCut", "AvoidAGrabCutEasy");
    
            return Type.GetType(String.Format("{0}, {1}", typeName, assemblyName));
        }
    }
}