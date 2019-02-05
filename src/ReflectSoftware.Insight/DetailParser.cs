using Plato.Extensions;
using ReflectSoftware.Insight.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

// Listeners strings can be a combination of the following:
// 
//	  1) Viewer, File[path=c:\log.rlg]
//	  2) File[path=c:\log1.rlg], File[path=d:\log2.rlg]
//	  3) Viewer[], UserDefinedListener1, UserDefinedListener2[ userParam1=1234; userParam2=ReflectInsight is great! ]
//
// Notes: 
//    1) Listener names must not contain spaces.
//    2) A listener with [] only is equal to no []. For example, Viewer[] is the same as Viewer. 
//    3) If more than one listener is defined in the detail string, they must be separated by comma (,)
//    4) All parameters must be contained within the square brackets (i.e. []) and must be in a key/value pair (i.e. path=c:\mylog.txt)
//    5) Multiple parameters within a listener must be separated by semi-colon (;) (i.e. MyListener[ userParam1=1234; userParam2=some value ]

namespace ReflectSoftware.Insight
{
	static internal class DetailParser
	{
        private static String ReconstructDetailString(String objName, SafeNameValueCollection objParams)
		{
			StringBuilder rValue = new StringBuilder(objName.Trim());
			if( objParams.Count > 0 )
			{
				rValue.Append( "[ " );

                foreach (String key in objParams.AllKeys)
                    rValue.AppendFormat("{0}={1}; ", key, objParams[key]);

				rValue.Replace( "; ", " ", rValue.Length-2, 2 );
				rValue.Append( "]" );
			}

			return rValue.ToString().Trim();
		}
        
        static private void ValidateCorrectUseOfBrackets(String details, char startBracket, char endBracket)
        {
            // This method ensures that square brackets are formated and used properly

            Int32 bCount = 0;
            Int32 len = details.Length;
            Boolean bLookingForLeft = true;

            for (Int32 i = 0; i < len; i++)
            {
                if (String.Compare(details[i].ToString(), startBracket.ToString(), false) == 0)
                {
                    if (bLookingForLeft)
                    {
                        bLookingForLeft = false;
                        bCount++;
                        continue;
                    }

                    // we found a double left
                    throw new ReflectInsightException(String.Format("Was expecting a matching bracket: {0}", startBracket));
                }

                if (String.Compare(details[i].ToString(), endBracket.ToString(), false) == 0)
                {
                    if (!bLookingForLeft)
                    {
                        bLookingForLeft = true;
                        bCount--;
                        continue;
                    }

                    // we found a double right
                    throw new ReflectInsightException(String.Format("Was expecting a matching bracket: {0}", endBracket));
                }
            }

            if (bCount != 0)
            {
                throw new ReflectInsightException(String.Format("Unmatched '{0} {1}' found in destination details. Ensure configuration settings are correct.", startBracket, endBracket));
            }
        }
        
        static private String EnsureNoSpacesForListenerName(String details)
        {
            // extract listener name only
            String listenerName = details;
            Int32 idx = details.IndexOf('[', 0);
            if (idx >= 0)
                listenerName = details.Remove(idx, details.Length - idx).Trim();

            if (String.Compare(listenerName.Trim(), String.Empty, false) == 0)
                throw new ReflectInsightException("Listener name not provided");

            if (listenerName.Contains(" "))
                throw new ReflectInsightException("Listener names cannot contain spaces");

            return listenerName; 
        }
        
        static private String MaskSpecialSymbols(String details)
        {
            // any special symbol (i.e. ,|;) within round brackets or double quotes must be masked
            Int32 startBlock = details.IndexOf('(');
            Int32 endBlock = details.LastIndexOf(')');
            if (startBlock < endBlock && startBlock != -1)
            {
                String subString = details.Substring(startBlock, endBlock - startBlock + 1);
                details = details.Replace(subString, "!@#$%^");

                subString = subString.Replace(",", "%#@1%#@");
                subString = subString.Replace(";", "%#@2%#@");
                subString = subString.Replace("|", "%#@3%#@");
                subString = subString.Replace("[", "%#@4%#@");
                subString = subString.Replace("]", "%#@5%#@");

                details = details.Replace("!@#$%^", subString);
            }

            startBlock = details.IndexOf('"');
            endBlock = details.LastIndexOf('"');

            if (startBlock < endBlock && startBlock != -1)
            {
                String subString = details.Substring(startBlock, endBlock - startBlock + 1);
                details = details.Replace(subString, "!@#$%^");

                subString = subString.Replace(",", "%#@1%#@");
                subString = subString.Replace(";", "%#@2%#@");
                subString = subString.Replace("|", "%#@3%#@");
                subString = subString.Replace("[", "%#@4%#@");
                subString = subString.Replace("]", "%#@5%#@");

                details = details.Replace("!@#$%^", subString);
            }

            return details;
        }
        
        static private String UnmaskSpecialSymbols(String details)
        {
            // any special symbol (i.e. ,|;) within round brackets or double quotes must be unmasked
            // note: see how MaskSpecialSymbols masks symbols

            details = details.Replace("%#@1%#@", ",");
            details = details.Replace("%#@2%#@", ";");
            details = details.Replace("%#@3%#@", "|");
            details = details.Replace("%#@4%#@", "[");
            details = details.Replace("%#@5%#@", "]");

            return details;
        }
        
        static private SafeNameValueCollection GetParameters(String details)
        {
            SafeNameValueCollection parameters = new SafeNameValueCollection();

            String listenerName = EnsureNoSpacesForListenerName(details);
            
            Int32 length = details.Length;                       
            details = details.Replace(String.Format("{0}[", listenerName), String.Empty).Replace("]", String.Empty).Trim();
            if (details.Length == length)
                details = details.Replace(listenerName, String.Empty);
            
            if (String.Compare(details, String.Empty, false) != 0)
            {
                String[] keyValuePairs = details.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (String pair in keyValuePairs)
                {
                    String[] keyValues = pair.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                    if (keyValues.Length != 2)
                        throw new ReflectInsightException("Listener parameters must follow the Key Value Pair model (i.e. param1=value1");

                    if (keyValues[0].Trim().Contains(" "))
                        throw new ReflectInsightException("Parameter names cannot contain spaces");

                    parameters[keyValues[0].Trim()] = UnmaskSpecialSymbols(keyValues[1].Trim());
                }
            }

            return parameters;
        }
        
        static public ListenerInfo CreateListenerInfo(String listenerName, SafeNameValueCollection objParams)
		{		
			if( listenerName == null )
				throw new ArgumentNullException( "listenerName" );

			listenerName = listenerName.Trim();
					
			// Ensure that Listener Name has no spaces. If so, raise exception
			if( listenerName.IndexOf(' ') > 0 || listenerName.Length == 0 )
			{                
                throw new ReflectInsightException(String.Format("'{0}' is not a proper listener name.", listenerName));
			}

            ListenerInfo listener = null;
            IReflectInsightListener iListener = ListenerLoader.Get(listenerName);
            try
            {
                if (objParams == null)
                    objParams = new SafeNameValueCollection();
                
                listener = new ListenerInfo(listenerName, ReconstructDetailString(listenerName, objParams), objParams, iListener);
            }
            catch (Exception ex)
            {
                iListener.DisposeObject();                
                throw new ReflectInsightException(String.Format("Listener '{0}' caused an exception during the ReconstructDetailString section for method CreateListenerInfo() .", listenerName), ex);
            }
            
            // update % parameters if any
            try
            {
                iListener.UpdateParameterVariables(listener);
            }
            catch (Exception ex)
            {
                listener.DisposeObject();
                throw new ReflectInsightException(String.Format("Listener '{0}' caused an exception during the UpdateParameterVariables() method.", listenerName), ex);
            }

            return listener;
		}
        
        static public void AddListenersByDetails(List<ListenerInfo> listeners, String details)
		{
            if (string.IsNullOrWhiteSpace(details))
                return;

            try
            {
                try
                {                    
                    ValidateCorrectUseOfBrackets(details, '(', ')');
                    ValidateCorrectUseOfBrackets(details, '"', '"');

                    details = MaskSpecialSymbols(details);
                    String[] subDetails = details.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (String subDetail in subDetails)
                    {                        
                        ValidateCorrectUseOfBrackets(subDetail, '[', ']');
                        
                        listeners.Add(CreateListenerInfo(EnsureNoSpacesForListenerName(subDetail), GetParameters(subDetail)));
                    }
                }
                catch (ReflectInsightException)
                {
                    throw;
                }
                catch (Exception ex)
                {                    
                    throw new ReflectInsightException(String.Format("Invalid Listener Details Format: {0}", details), ex);
                }
            }
            catch (Exception ex)
            {
                RIExceptionManager.Publish(ex, "Failed during: DetailParser.AddListenersByDetails()");
            }
		}	
        
		static public String ReconstructDetailString(ArrayList listeners)
		{
			StringBuilder rValue = new StringBuilder();		

			foreach( ListenerInfo listenerObj in listeners )
			{
				if( rValue.Length != 0 ) 
                    rValue.Append(", ");

				rValue.Append(listenerObj.Details);
			}
			
			return rValue.ToString();
		}
	}
}




