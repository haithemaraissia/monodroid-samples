﻿
using System;
using Android;
using Android.App;
using Android.Content;
using Android.Gms.AppInvite;
using Android.Gms.Common;
using Android.Gms.Common.Apis;
using Android.OS;
using Android.Support.V7.App;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace AppInvite
{
	[Activity (Label = "@string/app_name", Theme = "@style/ThemeOverlay.MyDialogActivity")]			
	public class DeepLinkActivity : AppCompatActivity, IGoogleApiClientConnectionCallbacks, IGoogleApiClientOnConnectionFailedListener, View.IOnClickListener
	{
		static readonly string Tag = typeof(DeepLinkActivity).Name;

		IGoogleApiClient googleApiClient;

		Intent cachedInvitationIntent;

		protected override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);
			SetContentView (Resource.Layout.deep_link_activity);

			FindViewById (Resource.Id.button_ok).SetOnClickListener (this);

			googleApiClient = new GoogleApiClientBuilder (this)
				.AddConnectionCallbacks (this)
				.EnableAutoManage (this, 0, this)
				.AddApi (Android.Gms.AppInvite.AppInviteClass.API)
				.Build ();
		}

		protected override void OnStart ()
		{
			base.OnStart ();
			ProcessReferralIntent (Intent);
		}

		void ProcessReferralIntent (Intent intent)
		{
			if (!AppInviteReferral.HasReferral (intent)) {
				Log.Error (Tag, "Error: DeepLinkActivity Intent does not contain App Invite");
				return;
			}

			var invitationId = AppInviteReferral.GetInvitationId (intent);
			var deepLink = AppInviteReferral.GetDeepLink (intent);

			Log.Debug (Tag, "Found Referral: " + invitationId + ":" + deepLink);
			(FindViewById<TextView> (Resource.Id.deep_link_text)).Text = string.Format(deepLink, GetString (Resource.String.deep_link_fmt));
			(FindViewById<TextView> (Resource.Id.invitation_id_text)).Text =  string.Format(invitationId, GetString (Resource.String.invitation_id_fmt));

			if (googleApiClient.IsConnected) {
				UpdateInvitationStatus (intent);
			} else {
				Log.Warn (Tag, "Warning: GoogleAPIClient not connected, can't update invitation.");
				cachedInvitationIntent = intent;
			}
		}

		void UpdateInvitationStatus (Intent intent)
		{
			var invitationId = AppInviteReferral.GetInvitationId (intent);

			if (AppInviteReferral.IsOpenedFromPlayStore (intent)) {
				AppInviteClass.AppInviteApi.UpdateInvitationOnInstall (googleApiClient, invitationId);
			}

			AppInviteClass.AppInviteApi.ConvertInvitation (googleApiClient, invitationId);
		}

		public void OnConnected (Bundle connectionHint)
		{
			Log.Debug (Tag, "googleApiClient:onConnected");

			if (cachedInvitationIntent != null) {
				UpdateInvitationStatus (cachedInvitationIntent);
				cachedInvitationIntent = null;
			}
		}

		public void OnConnectionSuspended (int cause)
		{
			Log.Debug (Tag, "googleApiClient:onConnectionSuspended");
		}

		public void OnConnectionFailed (ConnectionResult result)
		{
			Log.Debug (Tag, "googleApiClient:onConnectionFailed:" + result.ErrorCode);
			if (result.ErrorCode == ConnectionResult.ApiUnavailable) {
				Log.Warn (Tag, "onConnectionFailed because an API was unavailable");
			}
		}

		public void OnClick (View v)
		{
			switch (v.Id) {
			case Resource.Id.button_ok:
				Finish ();
				break;
			}
		}
	}
}

