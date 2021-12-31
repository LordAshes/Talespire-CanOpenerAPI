using BepInEx;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace LordAshes
{
    public partial class CanOpenerAPIPlugin : BaseUnityPlugin
    {
        private static object activeObject = null;
        private static Dictionary<string,object> stasisObjects = new Dictionary<string, object>();
        private static bool outputJSON = true;

        public static string ProcessIncoming(string requests)
        {
            return ProcessCommands(requests.Replace("\r\n", "\n").Split('\n'));
        }

        public static string ProcessCommands(string[] requests)
        {
            object result = "";
            foreach (string requestString in requests)
            {
                Debug.Log("Can Opener API Plugin: Processing '" + requestString + "'");
                string request = requestString.Replace("{}", result.ToString());
                string operation = request.Split(' ')[0];
                string name = "";
                Type t = null;
                PropertyInfo p;
                FieldInfo f;
                MethodInfo m;
                Debug.Log("Can Opener API Plugin: Processing '" + request + "'");
                switch (operation.ToUpper())
                {
                    case "TYPE":
                        name = request.Substring(request.IndexOf(" ") + 1);
                        activeObject = Type.GetType(name);
                        break;
                    case "NEW":
                        name = request.Substring(request.IndexOf(" ") + 1);
                        t = Type.GetType(name);
                        activeObject = Activator.CreateInstance(t);
                        break;
                    case "FIND":
                        name = request.Split(' ')[1];
                        if (name.ToUpper() == "LOCALCLIENT")
                        {
                            LocalClient temp;
                            LocalClient.TryGetInstance(out temp);
                            activeObject = temp;
                        }
                        else if (name.ToUpper() == "CREATUREPRESENTER")
                        {
                            CreaturePresenter temp;
                            CreaturePresenter.TryGetInstance(out temp);
                            activeObject = temp;
                        }
                        else if (name.ToUpper() == "CREATUREBOARDASSET")
                        {
                            CreatureGuid cid = new CreatureGuid(request.Split(' ')[2]);
                            CreatureBoardAsset temp;
                            CreaturePresenter.TryGetAsset(cid, out temp);
                            activeObject = temp;
                        }
                        else if (name.ToUpper() == "MESSAGES")
                        {
                            activeObject = StatMessaging.Guid;
                        }
                        else
                        {
                            activeObject = GameObject.Find(name);
                        }
                        break;
                    case "INSTANCES":
                        name = request.Substring(request.IndexOf(" ") + 1);
                        t = Type.GetType(name);
                        activeObject = GameObject.FindObjectsOfType(t);
                        break;
                    case "COMPONENTS":
                        name = request.Substring(request.IndexOf(" ") + 1);
                        t = Type.GetType(name);
                        activeObject = ((GameObject)activeObject).GetComponentInChildren(t);
                        break;
                    case ":":
                        activeObject = ((object[])activeObject)[int.Parse(name)];
                        break;
                    case ";":
                        activeObject = ((Dictionary<string, object>)activeObject)[name];
                        break;
                    case "SET":
                        name = request.Substring(request.IndexOf("(") + 1);
                        if (stasisObjects.ContainsKey(name)) { stasisObjects.Remove(name); }
                        stasisObjects.Add(name, activeObject);
                        break;
                    case "GET":
                        name = request.Substring(request.IndexOf(" ") + 1);
                        if (stasisObjects.ContainsKey(name)) { activeObject = stasisObjects[name]; } else { activeObject = null; }
                        break;
                    case "->":
                        name = request.Substring(request.IndexOf(" ") + 1);
                        t = activeObject.GetType();
                        p = t.GetProperty(name);
                        if (p != null)
                        {
                            activeObject = p.GetValue(activeObject);
                        }
                        else
                        {
                            f = t.GetField(name);
                            if (f != null)
                            {
                                activeObject = f.GetValue(activeObject);
                            }
                        }
                        break;
                    case "?":
                        name = request.Substring(request.IndexOf(" ") + 1);
                        Debug.Log("Object: "+activeObject.ToString());
                        if (activeObject.ToString() != StatMessaging.Guid)
                        {
                            Debug.Log("Property/Field Get");
                            t = activeObject.GetType();
                            p = t.GetProperty(name);
                            if (p != null)
                            {
                                result = p.GetValue(activeObject);
                            }
                            else
                            {
                                f = t.GetField(name);
                                if (f != null)
                                {
                                    result = f.GetValue(activeObject);
                                }
                            }
                        }
                        else
                        {
                            Debug.Log("Stat Messaging Get");
                            string[] valueParts = request.Split(' ');
                            CreatureBoardAsset asset;
                            CreaturePresenter.TryGetAsset(new CreatureGuid(valueParts[1]), out asset);
                            return StatMessaging.ReadInfo(asset.Creature.CreatureId, valueParts[2]);
                        }
                        break;
                    case "=":
                        name = request.Split(' ')[1];
                        if (activeObject.ToString() != StatMessaging.Guid)
                        {
                            string value = request.Split(' ')[2];
                            t = activeObject.GetType();
                            p = t.GetProperty(name);
                            if (p != null)
                            {
                                return SetPropertyValue(p, value);
                            }
                            else
                            {
                                f = t.GetField(name);
                                if (f != null)
                                {
                                    return SetFieldValue(f, value);
                                }
                            }
                        }
                        else
                        {
                            string[] valueParts = request.Split(' ');
                            CreatureBoardAsset asset;
                            CreaturePresenter.TryGetAsset(new CreatureGuid(valueParts[1]), out asset);
                            StatMessaging.SetInfo(asset.Creature.CreatureId, valueParts[2], valueParts[3]);
                            return valueParts[3].ToString();
                        }
                        break;
                    case "|":
                        name = request.Split(' ')[1];
                        object[] parts = request.Split(' ');
                        parts = parts.Skip(2).ToArray();
                        if (parts == null)
                        {
                            t = activeObject.GetType();
                            m = t.GetMethod(name);
                            m.Invoke(activeObject, new object[] { });
                        }
                        else
                        {
                            t = activeObject.GetType();
                            m = t.GetMethod(name);
                            m.Invoke(activeObject, parts);
                        }
                        break;
                }
            }
            if (!outputJSON)
            { 
                return result.ToString(); 
            } 
            else 
            {
                try
                {
                    return JsonConvert.SerializeObject(result);
                }
                catch(Exception)
                {
                    return result.ToString();
                }
            }
        }

        private static string SetPropertyValue(PropertyInfo setter, string value)
        {
            object valueObject = null;
            Type vt = setter.GetValue(activeObject).GetType();
            Debug.Log("Setter: "+setter.GetType()+" / DataType: " + vt.ToString());
            if (vt.ToString()== "UnityEngine.Vector3")
            {
                string[] parts = value.Split(',');
                Debug.Log("Vector3 = "+parts[0] + "," + parts[1] + "," + parts[2]);
                valueObject = new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
            }
            else
            {
                Debug.Log("Value = "+value);
                valueObject = JsonConvert.DeserializeObject(value, vt);
            }
            setter.SetValue(activeObject, valueObject);
            return value;
        }

        private static string SetFieldValue(FieldInfo setter, string value)
        {
            object valueObject = null;
            Type vt = setter.GetValue(activeObject).GetType();
            Debug.Log("Setter: " + setter.GetType() + " / DataType: " + vt.ToString());
            if (vt.ToString() == "UnityEngine.Vector3")
            {
                string[] parts = value.Split(',');
                Debug.Log("Vector3 = " + parts[0] + "," + parts[1] + "," + parts[2]);
                valueObject = new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
            }
            else
            {
                Debug.Log("Value = " + value);
                valueObject = JsonConvert.DeserializeObject(value, vt);
            }
            setter.SetValue(activeObject, valueObject);
            return value;
        }
    }
}
