#if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
//------------------------------------------------------------------------------
// <auto-generated />
//
// This file was automatically generated by SWIG (https://www.swig.org).
// Version 4.3.0
//
// Do not make changes to this file unless you know what you are doing - modify
// the SWIG interface file instead.
//------------------------------------------------------------------------------
public class AkInitSettings : global::System.IDisposable {
  private global::System.IntPtr swigCPtr;
  protected bool swigCMemOwn;
  internal AkInitSettings(global::System.IntPtr cPtr, bool cMemoryOwn) {
    swigCMemOwn = cMemoryOwn;
    swigCPtr = cPtr;
  }
  internal static global::System.IntPtr getCPtr(AkInitSettings obj) {
    return (obj == null) ? global::System.IntPtr.Zero : obj.swigCPtr;
  }
  internal virtual void setCPtr(global::System.IntPtr cPtr) {
    Dispose();
    swigCPtr = cPtr;
  }
  ~AkInitSettings() {
    Dispose(false);
  }
  public void Dispose() {
    Dispose(true);
    global::System.GC.SuppressFinalize(this);
  }
  protected virtual void Dispose(bool disposing) {
    lock(this) {
      if (swigCPtr != global::System.IntPtr.Zero) {
        if (swigCMemOwn) {
          swigCMemOwn = false;
          AkUnitySoundEnginePINVOKE.CSharp_delete_AkInitSettings(swigCPtr);
        }
        swigCPtr = global::System.IntPtr.Zero;
      }
      global::System.GC.SuppressFinalize(this);
    }
  }
  public uint uMaxNumPaths { set { AkUnitySoundEnginePINVOKE.CSharp_AkInitSettings_uMaxNumPaths_set(swigCPtr, value); }  get { return AkUnitySoundEnginePINVOKE.CSharp_AkInitSettings_uMaxNumPaths_get(swigCPtr); } 
  }
  public uint uCommandQueueSize { set { AkUnitySoundEnginePINVOKE.CSharp_AkInitSettings_uCommandQueueSize_set(swigCPtr, value); }  get { return AkUnitySoundEnginePINVOKE.CSharp_AkInitSettings_uCommandQueueSize_get(swigCPtr); } 
  }
  public bool bEnableGameSyncPreparation { set { AkUnitySoundEnginePINVOKE.CSharp_AkInitSettings_bEnableGameSyncPreparation_set(swigCPtr, value); }  get { return AkUnitySoundEnginePINVOKE.CSharp_AkInitSettings_bEnableGameSyncPreparation_get(swigCPtr); } 
  }
  public uint uContinuousPlaybackLookAhead { set { AkUnitySoundEnginePINVOKE.CSharp_AkInitSettings_uContinuousPlaybackLookAhead_set(swigCPtr, value); }  get { return AkUnitySoundEnginePINVOKE.CSharp_AkInitSettings_uContinuousPlaybackLookAhead_get(swigCPtr); } 
  }
  public uint uNumSamplesPerFrame { set { AkUnitySoundEnginePINVOKE.CSharp_AkInitSettings_uNumSamplesPerFrame_set(swigCPtr, value); }  get { return AkUnitySoundEnginePINVOKE.CSharp_AkInitSettings_uNumSamplesPerFrame_get(swigCPtr); } 
  }
  public uint uMonitorQueuePoolSize { set { AkUnitySoundEnginePINVOKE.CSharp_AkInitSettings_uMonitorQueuePoolSize_set(swigCPtr, value); }  get { return AkUnitySoundEnginePINVOKE.CSharp_AkInitSettings_uMonitorQueuePoolSize_get(swigCPtr); } 
  }
  public uint uCpuMonitorQueueMaxSize { set { AkUnitySoundEnginePINVOKE.CSharp_AkInitSettings_uCpuMonitorQueueMaxSize_set(swigCPtr, value); }  get { return AkUnitySoundEnginePINVOKE.CSharp_AkInitSettings_uCpuMonitorQueueMaxSize_get(swigCPtr); } 
  }
  public AkOutputSettings settingsMainOutput { set { AkUnitySoundEnginePINVOKE.CSharp_AkInitSettings_settingsMainOutput_set(swigCPtr, AkOutputSettings.getCPtr(value)); } 
    get {
      global::System.IntPtr cPtr = AkUnitySoundEnginePINVOKE.CSharp_AkInitSettings_settingsMainOutput_get(swigCPtr);
      AkOutputSettings ret = (cPtr == global::System.IntPtr.Zero) ? null : new AkOutputSettings(cPtr, false);
      return ret;
    } 
  }
  public uint uMaxHardwareTimeoutMs { set { AkUnitySoundEnginePINVOKE.CSharp_AkInitSettings_uMaxHardwareTimeoutMs_set(swigCPtr, value); }  get { return AkUnitySoundEnginePINVOKE.CSharp_AkInitSettings_uMaxHardwareTimeoutMs_get(swigCPtr); } 
  }
  public bool bUseSoundBankMgrThread { set { AkUnitySoundEnginePINVOKE.CSharp_AkInitSettings_bUseSoundBankMgrThread_set(swigCPtr, value); }  get { return AkUnitySoundEnginePINVOKE.CSharp_AkInitSettings_bUseSoundBankMgrThread_get(swigCPtr); } 
  }
  public bool bUseLEngineThread { set { AkUnitySoundEnginePINVOKE.CSharp_AkInitSettings_bUseLEngineThread_set(swigCPtr, value); }  get { return AkUnitySoundEnginePINVOKE.CSharp_AkInitSettings_bUseLEngineThread_get(swigCPtr); } 
  }
  public string szPluginDLLPath { set { AkUnitySoundEnginePINVOKE.CSharp_AkInitSettings_szPluginDLLPath_set(swigCPtr, value); }  get { return AkUnitySoundEngine.StringFromIntPtrOSString(AkUnitySoundEnginePINVOKE.CSharp_AkInitSettings_szPluginDLLPath_get(swigCPtr)); } 
  }
  public AkFloorPlane eFloorPlane { set { AkUnitySoundEnginePINVOKE.CSharp_AkInitSettings_eFloorPlane_set(swigCPtr, (int)value); }  get { return (AkFloorPlane)AkUnitySoundEnginePINVOKE.CSharp_AkInitSettings_eFloorPlane_get(swigCPtr); } 
  }
  public float fGameUnitsToMeters { set { AkUnitySoundEnginePINVOKE.CSharp_AkInitSettings_fGameUnitsToMeters_set(swigCPtr, value); }  get { return AkUnitySoundEnginePINVOKE.CSharp_AkInitSettings_fGameUnitsToMeters_get(swigCPtr); } 
  }
  public uint uBankReadBufferSize { set { AkUnitySoundEnginePINVOKE.CSharp_AkInitSettings_uBankReadBufferSize_set(swigCPtr, value); }  get { return AkUnitySoundEnginePINVOKE.CSharp_AkInitSettings_uBankReadBufferSize_get(swigCPtr); } 
  }
  public float fDebugOutOfRangeLimit { set { AkUnitySoundEnginePINVOKE.CSharp_AkInitSettings_fDebugOutOfRangeLimit_set(swigCPtr, value); }  get { return AkUnitySoundEnginePINVOKE.CSharp_AkInitSettings_fDebugOutOfRangeLimit_get(swigCPtr); } 
  }
  public bool bDebugOutOfRangeCheckEnabled { set { AkUnitySoundEnginePINVOKE.CSharp_AkInitSettings_bDebugOutOfRangeCheckEnabled_set(swigCPtr, value); }  get { return AkUnitySoundEnginePINVOKE.CSharp_AkInitSettings_bDebugOutOfRangeCheckEnabled_get(swigCPtr); } 
  }
  public bool bOfflineRendering { set { AkUnitySoundEnginePINVOKE.CSharp_AkInitSettings_bOfflineRendering_set(swigCPtr, value); }  get { return AkUnitySoundEnginePINVOKE.CSharp_AkInitSettings_bOfflineRendering_get(swigCPtr); } 
  }
}
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_WEBPLAYER || UNITY_WII || UNITY_WIIU || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY) // Disable under unsupported platforms.
