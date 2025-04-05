using UnityEngine;
using UnityEngine.Localization.Components;

public class Descriptions : MonoBehaviour
{
    // 分别对应名字和描述的Localize String Event组件
    [SerializeField] private LocalizeStringEvent nameLocalizeEvent;
    [SerializeField] private LocalizeStringEvent descriptionLocalizeEvent;

    /// <summary>
    /// 显示描述。type为0时表示Item，为1时表示Building，
    /// 根据type选择正确的Localization Table，并把id转换为字符串赋值给TableEntryReference。
    /// </summary>
    /// <param name="id">对应的条目id</param>
    /// <param name="type">类型：0代表Item，1代表Building</param>
    public void UpdateDescription(int id, int type)
    {
        // 将id转换为字符串
        string idStr = id.ToString();

        // 根据type选择对应的本地化表
        // 如果type为0（Item）则使用Item_Name和Item_Description，否则为Buildings_Name和Buildings_Description
        string nameTable = (type == 0) ? "Item_Name" : "Buildings_Name";
        string descriptionTable = (type == 0) ? "Item_Description" : "Building_Description";

        // 对Localize String Event的StringReference重新赋值
        if(nameLocalizeEvent != null && descriptionLocalizeEvent != null)
        {
            // 设置名字对应的本地化字符串
            nameLocalizeEvent.StringReference.TableReference = nameTable;
            nameLocalizeEvent.StringReference.TableEntryReference = idStr;

            // 设置描述对应的本地化字符串
            descriptionLocalizeEvent.StringReference.TableReference = descriptionTable;
            descriptionLocalizeEvent.StringReference.TableEntryReference = idStr;
        }
        else
        {
            Debug.LogError("LocalizeStringEvent组件未正确设置,请检查引用！");
        }
    }
    /// <summary>
    /// 清除两个 LocalizeStringEvent 组件的引用, 以便不显示描述.
    /// </summary>
    public void ClearLocalizationReferences()
    {
        // 清除名字的 LocalizeStringEvent 的引用.
        if (nameLocalizeEvent != null)
        {
            nameLocalizeEvent.StringReference.TableReference = null;
            nameLocalizeEvent.StringReference.TableEntryReference = null;
            // 获取同一 GameObject 上的 TextMeshProUGUI 组件, 并清空文本.
            TMPro.TextMeshProUGUI tmpName = nameLocalizeEvent.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmpName != null)
            {
                tmpName.text = "";
            }
        }
        // 清除描述的 LocalizeStringEvent 的引用.
        if (descriptionLocalizeEvent != null)
        {
            descriptionLocalizeEvent.StringReference.TableReference = null;
            descriptionLocalizeEvent.StringReference.TableEntryReference = null;
            // 获取同一 GameObject 上的 TextMeshProUGUI 组件, 并清空文本.
            TMPro.TextMeshProUGUI tmpDesc = descriptionLocalizeEvent.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmpDesc != null)
            {
                tmpDesc.text = "";
            }
        }
    }
}
