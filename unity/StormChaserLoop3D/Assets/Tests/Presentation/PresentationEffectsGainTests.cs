using NUnit.Framework;
using UnityEngine;

public class PresentationEffectsGainTests
{
    [Test]
    public void MuteAndRestore_PreserveIndependentEnvelopesAndRefreshAfterReenable()
    {
        var root = new GameObject("EffectsGainTest");
        var loop = root.AddComponent<AudioSource>();
        var shot = root.AddComponent<AudioSource>();
        var gain = new PresentationEffectsGain();
        float saved = GameAudio.EffectsVolume;
        try
        {
            GameAudio.EffectsVolume = 1f;
            gain.Set(loop, 0.6f); gain.Set(shot, 0.4f); gain.Enable(); gain.Enable();
            GameAudio.EffectsVolume = 0f;
            Assert.That(loop.volume, Is.Zero); Assert.That(shot.volume, Is.Zero);
            gain.Set(loop, 0.8f); // Envelope continues changing while muted.
            GameAudio.EffectsVolume = 0.5f;
            Assert.That(loop.volume, Is.EqualTo(0.4f).Within(0.0001f));
            Assert.That(shot.volume, Is.EqualTo(0.2f).Within(0.0001f));
            Assert.That(gain.Get(loop), Is.EqualTo(0.8f));
            gain.Disable(); gain.Disable(); GameAudio.EffectsVolume = 1f;
            Assert.That(loop.volume, Is.EqualTo(0.4f).Within(0.0001f), "Disabled owners must detach.");
            gain.Enable();
            Assert.That(loop.volume, Is.EqualTo(0.8f).Within(0.0001f));
            Assert.That(shot.volume, Is.EqualTo(0.4f).Within(0.0001f));
        }
        finally { gain.Disable(); GameAudio.EffectsVolume = saved; Object.DestroyImmediate(root); }
    }
}
