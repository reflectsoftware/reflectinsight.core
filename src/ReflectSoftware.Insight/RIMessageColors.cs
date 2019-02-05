using ReflectSoftware.Insight.Common;
using System;
using System.Collections;
using System.Drawing;

namespace ReflectSoftware.Insight
{
    static public class RIMessageColors
    {
        static private Hashtable MessageColors;
        
        static RIMessageColors()
        {
            MessageColors = new Hashtable();
            ReflectInsightService.Initialize();
        }

        static internal void OnConfigFileChange()
        {
            try
            {
                lock (MessageColors)
                {
                    LoadConfigColors();
                }
            }
            catch (Exception ex)
            {
                RIExceptionManager.Publish(ex, "Failed during: static RIMessageColors.OnConfigFileChange()");
            }
        }

        static internal void OnStartup()
        {
            OnConfigFileChange();
        }
        
        static internal void OnShutdown()
        {
            if (MessageColors != null)
            {
                MessageColors.Clear();
                MessageColors = null;
            }
        }

        static private void LoadConfigColors()        
        {
            lock (MessageColors)
            {
                MessageColors.Clear();
                Type msgType = typeof(MessageType);

                Hashtable configMessageColors = ReflectInsightConfig.Settings.LoadMessageColors();
                foreach (String mType in configMessageColors.Keys)
                {
                    if (!Enum.IsDefined(msgType, mType))
                    {
                        continue;
                    }

                    MessageColors[Enum.Parse(msgType, mType)] = RIPastelBackColor.GetColorByName((String)configMessageColors[mType]);
                }
            }
        }
        
        static public void Clear()
        {
            lock (MessageColors) MessageColors.Clear();
        }

        static public void SetBackColor(MessageType mType, Color bkColor)
        {
            lock(MessageColors) MessageColors[mType] = bkColor;
        }

        static public Color GetBackColor(MessageType mType, Color defaultColor)
        {
            lock (MessageColors) return MessageColors[mType] != null ? (Color)MessageColors[mType] : defaultColor;
        }
    }
}
