Security
========


Verifying SyncTrayzor Releases
------------------------------

How do you know that the SyncTrayzor release you're downloading was actually built by me, and that my GitHub account hasn't been compromised?

Every release is accompanied by a `sha1sum.txt.asc` file.
This contains the sha1sum of all released files, and a PGP signature.
That signature was created by my private release key (fingerprint `FE6ADC8AE112FA6A`), which was signed by Syncthing's release key.
Finally, Syncthing's release key is available on the [Syncthing Security](https://syncthing.net/security.html) page.

This means that you can verify a release file by performing the following steps:

Once-off:

1. Visit the [Syncthing Security](https://syncthing.net/security.html) page, and verify that the fingerprint for the Syncthing release key is `D26E6ED000654A3E`.
2. Download the Syncthing release key (`D26E6ED000654A3E`) and the SyncTrayzor release key (`FE6ADC8AE112FA6A`) into your keychain.
3. Verify that the SyncTrayzor release key was signed by the Syncthing release key.

For every release:

1. Download the release file you're interested in, and the `sha1sum.txt.asc` file.
2. Verify that the `sha1sum.txt.asc` file was signed by the SyncTrayzor release key.
3. Verify that the sha1 hash of the release file you downloaded matches the value in `sha1sum.txt.asc`.

For example:

```
# Download the Syncthing release key and SyncTrayzor release key into your keychain

antony@creek ~ $ gpg2 --recv-key D26E6ED000654A3E FE6ADC8AE112FA6A
gpg: key E112FA6A: public key "SyncTrayzor Release Management <antony.male@gmail.com>" imported
gpg: key 00654A3E: public key "Syncthing Release Management <release@syncthing.net>" imported
gpg: no ultimately trusted keys found
gpg: Total number processed: 2
gpg:               imported: 2

# Verify that SyncTrayzor's release key is signed by Syncthing's release key

antony@creek ~ $ gpg2 --check-sigs FE6ADC8AE112FA6A
3 signatures not checked due to missing keys
pub   rsa2048/E112FA6A 2015-06-18
uid       [ unknown] SyncTrayzor Release Management <antony.male@gmail.com>
sig!3        E112FA6A 2015-06-18  SyncTrayzor Release Management <antony.male@gmail.com>
sig!         00654A3E 2015-06-18  Syncthing Release Management <release@syncthing.net>

# ^ IMPORTANT! This should say 'sig!'

# Check the signature on sha1sum.txt.asc

antony@creek ~ $ gpg2 --verify sha1sum.txt.asc
gpg: Signature made Sat 20 Jun 2015 23:22:45 BST using RSA key ID E112FA6A
gpg: Good signature from "SyncTrayzor Release Management <antony.male@gmail.com>" [unknown]
gpg: WARNING: This key is not certified with a trusted signature!
gpg:          There is no indication that the signature belongs to the owner.
Primary key fingerprint: A9C1 9402 0929 AA7B B1D1  C9C6 FE6A DC8A E112 FA6A

# The important line here is
# << gpg: Good signature from "SyncTrayzor Release Management <antony.male@gmail.com>" [unknown] >>

# Verify the checksum of the release file you downloaded
# Errors will be printed for the release files you did not download - these can be ignored.
# The important line is the one which corresponds to the release file you downloaded.

antony@creek ~ $ sha1sum -c sha1sum.txt.asc
...
SyncTrayzorSetup-x86.exe: OK
...
sha1sum: WARNING: 17 lines are improperly formatted
sha1sum: WARNING: 3 listed files could not be read
```


Automatic Update Security
-------------------------

Every automatically downloaded update is verified in a similar way to the procedure outlined above.

SyncTrayzor contains the certificate of the SyncTrayzor Release Key.
When it downloads an update, it will also download the `sha1sum.txt.asc` file for that release.
It will then verify signature on the `sha1sum.txt.asc` file using the certificate it has, before checking that the sha1sum of the downloaded update matches that in the `sha1sum.txt.asc` file.

If either of these checks fails, then both files are deleted.

This means that only updates which are 1) not corrupt, and 2) were signed by the SyncTrayzor release private key are installed.

As part of the build process, Syncthing binaries are downloaded and are bundled with the SyncTrayzor installer.
A similar check is carried out here: SyncTrayzor contains Syncthing's release key, and verifies that the Syncthing binaries were released by the owner of that key.