using NUnit.Framework;
using UnityEngine;
using AuraFarming.Editor;

namespace AuraFarming.Tests.SceneSetup
{
    public sealed class AuraWindowBridgeProtocolTests
    {
        [Test] public void Protocol_RegistersRequiredReadOnlyCommands()
        {
            Assert.IsTrue(AuraWindowBridgeServer.IsKnownCommand("ping"));
            Assert.IsTrue(AuraWindowBridgeServer.IsKnownCommand("getHierarchy"));
            Assert.IsTrue(AuraWindowBridgeServer.IsKnownCommand("getConsole"));
            Assert.IsFalse(AuraWindowBridgeServer.IsKnownCommand("deleteProject"));
        }
        [Test] public void StablePath_UsesRootToLeafNames()
        {
            var root = new GameObject("BridgeRoot"); var child = new GameObject("BridgeChild"); child.transform.SetParent(root.transform);
            Assert.AreEqual("BridgeRoot/BridgeChild", AuraWindowBridgeServer.StablePath(child));
            Object.DestroyImmediate(root);
        }
    }
}
