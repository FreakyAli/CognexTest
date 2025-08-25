<h1>Add your key to the Info.plist and AndroidManifest.xml </h1>

Info.plist
```
<key>MX_MOBILE_LICENSE</key>
<string>Your license key</string>
```

AndroidManifest.xml

```
<application >
		<meta-data android:name="MX_MOBILE_LICENSE" 
          android:value="YOUR_MX_MOBILE_LICENSE"/>
	</application>
```
