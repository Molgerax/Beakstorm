/////////////////////////////////////////////////////////////////////////////////////////////////////
//
// Audiokinetic Wwise generated include file. Do not edit.
//
/////////////////////////////////////////////////////////////////////////////////////////////////////

#ifndef __WWISE_IDS_H__
#define __WWISE_IDS_H__

#include <AK/SoundEngine/Common/AkTypes.h>

namespace AK
{
    namespace EVENTS
    {
        static const AkUniqueID PLAY_BIRDATTACK = 2435460859U;
        static const AkUniqueID PLAY_BIRDFOLLOW = 217586636U;
        static const AkUniqueID PLAY_BIRDSTATIC = 4227130709U;
        static const AkUniqueID PLAY_BIRDTARGET_DEPRECATED = 4054685192U;
        static const AkUniqueID PLAY_DAMAGE = 784302017U;
        static const AkUniqueID PLAY_GLIDERHUM = 1912775917U;
        static const AkUniqueID PLAY_LEVELATMO = 1661723931U;
        static const AkUniqueID PLAY_LEVELMUSIC = 2671044069U;
        static const AkUniqueID PLAY_MENUATMO = 3830652644U;
        static const AkUniqueID PLAY_MENUBACK = 1113566242U;
        static const AkUniqueID PLAY_MENUCHOOSE = 1897074628U;
        static const AkUniqueID PLAY_MENUCONFIRM = 3100142473U;
        static const AkUniqueID PLAY_MENUMUSIC_DEPRECATED = 530752644U;
        static const AkUniqueID PLAY_PHERODRAGLOOP = 854875926U;
        static const AkUniqueID PLAY_PHERODRAGLOOPSTOP = 1742926398U;
        static const AkUniqueID PLAY_PHERODRAGSTART = 4119062480U;
        static const AkUniqueID PLAY_PHERODRAGSTOP = 3327444668U;
        static const AkUniqueID PLAY_PHEROSHOOT = 4261851675U;
        static const AkUniqueID PLAY_SHIPCANNONBIG = 1568592687U;
        static const AkUniqueID PLAY_SHIPCANNONSMALL = 3378048096U;
        static const AkUniqueID PLAY_SHIPDESTROYBIG = 1353941456U;
        static const AkUniqueID PLAY_SHIPDESTROYSMALL = 1935674591U;
        static const AkUniqueID PLAY_SHIPHORN_DEPRECATED = 4218965445U;
        static const AkUniqueID PLAY_SHIPSTATICBIG = 236989436U;
        static const AkUniqueID PLAY_SHIPSTATICSMALL = 264564891U;
        static const AkUniqueID STOP_ALL = 452547817U;
    } // namespace EVENTS

    namespace STATES
    {
        namespace WAVE_STATE
        {
            static const AkUniqueID GROUP = 108563492U;

            namespace STATE
            {
                static const AkUniqueID NONE = 748895195U;
                static const AkUniqueID PEACE1 = 1350254358U;
                static const AkUniqueID PEACE2 = 1350254357U;
                static const AkUniqueID PEACE3 = 1350254356U;
                static const AkUniqueID PEACE4 = 1350254355U;
                static const AkUniqueID PEACE5 = 1350254354U;
                static const AkUniqueID WAR1 = 1873893370U;
                static const AkUniqueID WAR2 = 1873893369U;
                static const AkUniqueID WAR3 = 1873893368U;
                static const AkUniqueID WAR4 = 1873893375U;
                static const AkUniqueID WAR5 = 1873893374U;
            } // namespace STATE
        } // namespace WAVE_STATE

        namespace WAVE_TYPE
        {
            static const AkUniqueID GROUP = 3590821249U;

            namespace STATE
            {
                static const AkUniqueID NONE = 748895195U;
                static const AkUniqueID PEACE = 103389341U;
                static const AkUniqueID WAR = 1113986025U;
            } // namespace STATE
        } // namespace WAVE_TYPE

    } // namespace STATES

    namespace GAME_PARAMETERS
    {
        static const AkUniqueID DOPPLERPITCH = 331339483U;
        static const AkUniqueID PLAYER_HEALTH = 215992295U;
        static const AkUniqueID PLAYER_SPEED = 1062779386U;
        static const AkUniqueID PLAYER_THRUST = 2560771683U;
        static const AkUniqueID VOLUME_BIRDS = 1845579566U;
        static const AkUniqueID VOLUME_MASTER = 3695994288U;
        static const AkUniqueID VOLUME_MUSIC = 3891337659U;
        static const AkUniqueID VOLUME_SFX = 3673881719U;
    } // namespace GAME_PARAMETERS

    namespace TRIGGERS
    {
        static const AkUniqueID SHIP_SINK = 2111840295U;
        static const AkUniqueID SHIP_SPAWN = 953668639U;
    } // namespace TRIGGERS

    namespace BANKS
    {
        static const AkUniqueID INIT = 1355168291U;
        static const AkUniqueID MASTER = 4056684167U;
    } // namespace BANKS

    namespace BUSSES
    {
        static const AkUniqueID _2D_SFX = 1191978290U;
        static const AkUniqueID _3D_MUSIC = 2352922065U;
        static const AkUniqueID _3D_SF = 2123328431U;
        static const AkUniqueID _3D_SF_BIRDS = 4076533690U;
        static const AkUniqueID MASTER = 4056684167U;
        static const AkUniqueID MUSIC_AMBI = 2660909127U;
        static const AkUniqueID SFX_AMBI = 644840319U;
        static const AkUniqueID SFX_AMBI_BIRDS = 3904829738U;
    } // namespace BUSSES

    namespace AUX_BUSSES
    {
        static const AkUniqueID REVERB = 348963605U;
    } // namespace AUX_BUSSES

    namespace AUDIO_DEVICES
    {
        static const AkUniqueID ASIO_OUTPUT = 2712377323U;
        static const AkUniqueID NO_OUTPUT = 2317455096U;
        static const AkUniqueID SYSTEM = 3859886410U;
    } // namespace AUDIO_DEVICES

}// namespace AK

#endif // __WWISE_IDS_H__
