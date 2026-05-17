using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Graphite.Dialog;

public class GraphiteGraphPlayer : MonoBehaviour
{
    public DialogGraphContainer graphContainer;

    void Start()
    {
        if (graphContainer == null)
        {
            Debug.LogError("GraphiteGraphPlayer: no DialogGraphContainer assigned!", this);
            return;
        }

        var sys = DialogSystem.Instance;
        if (sys == null)
        {
            Debug.LogError("GraphiteGraphPlayer: no DialogSystem found in scene!", this);
            return;
        }

        PlayGraph(sys);
    }

    void PlayGraph(DialogSystem sys)
    {
        var so = ScriptableObject.CreateInstance<DialogSO>();
        so.actorName = graphContainer.name;
        so.entries = TraverseGraph();
        sys.StartDialog(so);
    }

    List<DialogEntry> TraverseGraph()
    {
        var entries = new List<DialogEntry>();

        string currentGuid = "ENTRYPOINT";
        int safety = 0;

        while (safety < 200)
        {
            safety++;
            var link = graphContainer.nodeLinks.FirstOrDefault(l => l.baseNodeGuid == currentGuid && l.portName == "DEFAULT");
            if (link == null) break;

            currentGuid = link.targetNodeGuid;
            var nodeData = graphContainer.nodeData.FirstOrDefault(n => n.GUID == currentGuid);
            if (nodeData == null) break;

            if (nodeData is SerializedDialogNode dialogNode)
            {
                var optionNodes = FindConnectedOptionNodes(dialogNode.GUID);

                var options = optionNodes.Select(o =>
                {
                    var responseGuid = GetConnectedNode(o.GUID, "DEFAULT");
                    var dialogs = new List<DialogEntry>();
                    if (responseGuid != null)
                    {
                        var responseNode = graphContainer.nodeData.FirstOrDefault(n => n.GUID == responseGuid) as SerializedResponseNode;
                        if (responseNode != null)
                        {
                            dialogs.Add(new DialogEntry
                            {
                                character = responseNode.character,
                                dialogText = responseNode.responseText,
                            });
                        }
                        dialogs.AddRange(TraverseSubGraph(responseGuid));
                    }
                    return new DialogOption
                    {
                        optionText = o.optionText,
                        responseDialogs = dialogs,
                    };
                }).ToList();

                var entry = new DialogEntry
                {
                    character = CharacterName.Maya,
                    dialogText = dialogNode.dialogText,
                    hasOptions = options.Count > 0,
                    options = options,
                    customWaveColor = Color.white,
                    customAlertColor = Color.white,
                };
                entries.Add(entry);

                if (entry.hasOptions) break;
            }
            else if (nodeData is SerializedExitNode)
            {
                break;
            }
            else if (nodeData is SerializedOptionNode || nodeData is SerializedResponseNode)
            {
                currentGuid = GetConnectedNode(currentGuid, "DEFAULT");
            }
            else
            {
                currentGuid = GetConnectedNode(currentGuid, "DEFAULT");
            }
        }

        return entries;
    }

    List<DialogEntry> TraverseSubGraph(string startGuid)
    {
        var entries = new List<DialogEntry>();
        string currentGuid = startGuid;
        int safety = 0;

        while (safety < 200)
        {
            safety++;
            var link = graphContainer.nodeLinks.FirstOrDefault(l => l.baseNodeGuid == currentGuid && l.portName == "DEFAULT");
            if (link == null) break;

            currentGuid = link.targetNodeGuid;
            var nodeData = graphContainer.nodeData.FirstOrDefault(n => n.GUID == currentGuid);
            if (nodeData == null) break;

            if (nodeData is SerializedDialogNode dialogNode)
            {
                var subOptionNodes = FindConnectedOptionNodes(dialogNode.GUID);
                var hasSubOptions = subOptionNodes.Count > 0;

                if (hasSubOptions)
                {
                    var subOptions = subOptionNodes.Select(o =>
                    {
                        var responseGuid = GetConnectedNode(o.GUID, "DEFAULT");
                        var dialogs = new List<DialogEntry>();
                        if (responseGuid != null)
                        {
                            var responseNode = graphContainer.nodeData.FirstOrDefault(n => n.GUID == responseGuid) as SerializedResponseNode;
                            if (responseNode != null)
                            {
                                dialogs.Add(new DialogEntry
                                {
                                    character = responseNode.character,
                                    dialogText = responseNode.responseText,
                                });
                            }
                            dialogs.AddRange(TraverseSubGraph(responseGuid));
                        }
                        return new DialogOption
                        {
                            optionText = o.optionText,
                            responseDialogs = dialogs,
                        };
                    }).ToList();

                    entries.Add(new DialogEntry
                    {
                        character = CharacterName.Maya,
                        dialogText = dialogNode.dialogText,
                        hasOptions = true,
                        options = subOptions,
                        customWaveColor = Color.white,
                        customAlertColor = Color.white,
                    });
                    break;
                }

                entries.Add(new DialogEntry
                {
                    character = CharacterName.Maya,
                    dialogText = dialogNode.dialogText,
                    hasOptions = false,
                    options = new List<DialogOption>(),
                    customWaveColor = Color.white,
                    customAlertColor = Color.white,
                });
            }
            else if (nodeData is SerializedExitNode)
            {
                break;
            }
            else if (nodeData is SerializedOptionNode || nodeData is SerializedResponseNode)
            {
                currentGuid = GetConnectedNode(currentGuid, "DEFAULT");
            }
            else
            {
                currentGuid = GetConnectedNode(currentGuid, "DEFAULT");
            }
        }

        return entries;
    }

    List<SerializedOptionNode> FindConnectedOptionNodes(string baseGuid)
    {
        var results = new List<SerializedOptionNode>();
        foreach (var link in graphContainer.nodeLinks)
        {
            if (link.baseNodeGuid != baseGuid || link.portName == "DEFAULT") continue;
            var node = graphContainer.nodeData.FirstOrDefault(n => n.GUID == link.targetNodeGuid);
            if (node is SerializedOptionNode opt)
                results.Add(opt);
        }
        return results;
    }

    string GetConnectedNode(string baseGuid, string portName)
    {
        var link = graphContainer.nodeLinks.FirstOrDefault(l => l.baseNodeGuid == baseGuid && l.portName == portName);
        return link?.targetNodeGuid;
    }
}
