using Plato.Extensions;
using ReflectSoftware.Insight.Common;
using System;
using System.Collections.Specialized;

namespace ReflectSoftware.Insight
{
    static public class ListenerFileHelper
    {        
        static public MessageTextFlag DetermineMessageTextFlagParam(IListenerInfo listener)
        {
            try
            {
                MessageTextFlag msgFlags = MessageTextFlag.None;
                Type msgTextFlagType = typeof(MessageTextFlag);

                String details = listener.Params["messageDetails"].FullTrim();
                if (details != String.Empty)
                {
                    String[] flags = details.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (String flag in flags)
                    {
                        if (Enum.IsDefined(msgTextFlagType, flag))
                        {
                            msgFlags = msgFlags | (MessageTextFlag)Enum.Parse(msgTextFlagType, flag);
                        }
                    }
                }

                return msgFlags;
            }
            catch (Exception ex)
            {
                throw new ReflectInsightException(String.Format("Error reading message detail parameter configuration values for Listener: '{0}' using details: '{1}'.", listener.Name, listener.Details), ex);
            }
        }

        static public String DetermineMessageTextPattern(IListenerInfo listener)
        {
            String pattern = "%message%";
            String patternName = listener.Params["messagePattern"];
            if (!string.IsNullOrWhiteSpace(patternName))
            {
                NameValueCollection nvc = ReflectInsightConfig.Settings.GetSubsection(String.Format("messagePatterns.{0}", patternName));
                if (nvc != null)
                {
                    pattern = nvc["pattern"].IfNullOrEmptyUseDefault(pattern);
                }
            }

            return pattern.Replace("&#xA;", String.Empty).Replace("\n", String.Empty).Replace("%newline%", Environment.NewLine);
        }

        static public String DeterminePathParam(IListenerInfo listener)
        {
            return FileHelper.GetFileNameFromPattern(listener.Params["path"]);
        }

        static public RIAutoSaveInfo DetermineAutoSaveParam(IListenerInfo listener)
        {
            RIAutoSaveInfo autoSave = new RIAutoSaveInfo();

            try
            {
                // get auto save values
                String autoSaveName = listener.Params["autoSave"].Trim();
                NameValueCollection nvc = ReflectInsightConfig.Settings.GetSubsection(String.Format("autoSaves.{0}", autoSaveName));
                if (nvc == null)
                {
                    // try the default
                    autoSaveName = ReflectInsightConfig.Settings.GetFilesAttribute("default", String.Empty);
                    nvc = ReflectInsightConfig.Settings.GetSubsection(String.Format("autoSaves.{0}", autoSaveName));

                    if (nvc == null)
                        nvc = new NameValueCollection();
                }               

                autoSave.SaveOnNewDay = nvc["onNewDay"].IfNullOrEmptyUseDefault("false").ToLower() == "true";
                autoSave.SaveOnMsgLimit = 1000000;
                autoSave.RecycleFilesEvery = 0;
                autoSave.SaveOnSize = 0;

                Int32.TryParse(nvc["onMsgLimit"].IfNullOrEmptyUseDefault("1000000"), out autoSave.SaveOnMsgLimit);
                Int32.TryParse(nvc["onSize"].IfNullOrEmptyUseDefault("0"), out autoSave.SaveOnSize);
                Int16.TryParse(nvc["recycleFilesEvery"].IfNullOrEmptyUseDefault("0"), out autoSave.RecycleFilesEvery);


                const Int32 maxMessages = 1000000;
                if (autoSave.SaveOnMsgLimit > maxMessages)
                    autoSave.SaveOnMsgLimit = maxMessages;

                if( autoSave.SaveOnMsgLimit < 1000)
                    autoSave.SaveOnMsgLimit = 1000;

                const Int32 maxSize = 102400;
                if (autoSave.SaveOnSize > maxSize)
                    autoSave.SaveOnSize = maxSize;
                
                if (autoSave.SaveOnSize < 0)
                    autoSave.SaveOnSize = 0;
                
                if (autoSave.RecycleFilesEvery < 0)
                    autoSave.RecycleFilesEvery = 0;

                return autoSave;
            }
            catch (Exception ex)
            {                
                throw new ReflectInsightException(String.Format("Error reading AutoSave configuration values for Listener: '{0}' using details: '{1}'.", listener.Name, listener.Details), ex);
            }
        }
    }
}
