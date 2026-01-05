using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

public class DocketManager : MonoBehaviour
{
    [System.Serializable]
    public class DocketDisplay
    {
        public GameObject docketBackground; // Paper docket background image
        public Image tableNumberImage; // Image showing table number (1, 2, 3, etc)
        public Image orderImage; // Image showing the order (drink picture, etc)
    }

    [Header("Docket Display Settings")]
    public List<DocketDisplay> docketSlots = new List<DocketDisplay>(); // 3 slots for dockets

    [Header("Table Number Images")]
    public List<Sprite> tableNumberSprites = new List<Sprite>(); // Sprites for tables 1-9 or however many you have

    private Queue<Docket> docketQueue = new Queue<Docket>();

    private static DocketManager instance;

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);

        // Disable all docket slots on startup
        DisableAllDockets();
    }

    void DisableAllDockets()
    {
        foreach (var slot in docketSlots)
        {
            if (slot.docketBackground != null)
                slot.docketBackground.SetActive(false);
            if (slot.tableNumberImage != null)
                slot.tableNumberImage.gameObject.SetActive(false);
            if (slot.orderImage != null)
                slot.orderImage.gameObject.SetActive(false);
        }
    }

    public static DocketManager Get()
    {
        return instance;
    }

    public void AddDocket(Docket docket)
    {
        docketQueue.Enqueue(docket);
        UpdateDisplay();
    }

    public void RemoveDocket(Docket docket)
    {
        // Remove from queue (create new queue without this docket)
        Queue<Docket> newQueue = new Queue<Docket>();
        foreach (var d in docketQueue)
        {
            if (d != docket)
                newQueue.Enqueue(d);
        }
        docketQueue = newQueue;
        UpdateDisplay();
    }

    void UpdateDisplay()
    {
        // Show only the first 3 (oldest)
        Docket[] docketsToShow = new Docket[Mathf.Min(3, docketQueue.Count)];
        int index = 0;
        foreach (var docket in docketQueue)
        {
            if (index >= 3) break;
            docketsToShow[index] = docket;
            index++;
        }

        // Display them in the slots
        for (int i = 0; i < docketSlots.Count; i++)
        {
            DocketDisplay slot = docketSlots[i];

            if (i < docketsToShow.Length && docketsToShow[i] != null)
            {
                // Enable and show this slot with order info
                if (slot.docketBackground != null)
                    slot.docketBackground.SetActive(true);

                // Set table number image
                if (slot.tableNumberImage != null && docketsToShow[i].tableNumber > 0)
                {
                    int tableIndex = docketsToShow[i].tableNumber - 1;
                    if (tableIndex < tableNumberSprites.Count)
                    {
                        slot.tableNumberImage.sprite = tableNumberSprites[tableIndex];
                        slot.tableNumberImage.gameObject.SetActive(true);
                    }
                }

                // Set order image
                if (slot.orderImage != null && docketsToShow[i].orderImage != null)
                {
                    slot.orderImage.sprite = docketsToShow[i].orderImage;
                    slot.orderImage.gameObject.SetActive(true);
                }
            }
            else
            {
                // Disable this slot - no order for it
                if (slot.docketBackground != null)
                    slot.docketBackground.SetActive(false);
            }
        }
    }
}

public class Docket
{
    public int tableNumber;
    public string orderName;
    public GameObject orderObject;
    public Sprite orderImage; // The drink/food picture

    public Docket(int table, string order, GameObject orderObj, Sprite image)
    {
        tableNumber = table;
        orderName = order;
        orderObject = orderObj;
        orderImage = image;
    }
}