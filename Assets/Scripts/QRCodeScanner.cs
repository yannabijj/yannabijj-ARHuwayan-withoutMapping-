using UnityEngine;
using UnityEngine.UI;
using ZXing;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Networking;
using ZXing.Common;

public class QRCodeScanner : MonoBehaviour
{
    public RawImage scanZone;
    public Button qrCodeButton;
    public TMP_InputField searchInputField;
    public TMP_Dropdown searchDropdown;
    public GameObject qrCodeOverlay;

    private WebCamTexture camTexture;
    private bool scanning = false;
    private string apiUrl = "https://firm-polecat-neatly.ngrok-free.app/unityAR/getTargetCube.php";

    private List<string> filteredResults = new List<string>();


    public SetNavigationTarget navigationTargetHandler;

    void Start()
    {
        qrCodeButton.onClick.AddListener(StartScanning);
        scanZone.gameObject.SetActive(false);
        searchInputField.gameObject.SetActive(false);
        searchDropdown.gameObject.SetActive(false);
        qrCodeOverlay.SetActive(false);

        if (searchInputField != null)
        {
            searchInputField.onValueChanged.AddListener(OnSearchInputChanged);
            searchInputField.onSubmit.AddListener(OnSearchSubmit);
        }
        else
        {
            Debug.LogError("Search Input Field is not assigned.");
        }

        searchDropdown.onValueChanged.AddListener(OnDropdownSelectionChanged);
    }

    void StartScanning()
    {
        scanZone.gameObject.SetActive(true);
        camTexture = new WebCamTexture();
        scanZone.texture = camTexture;
        camTexture.Play();
        scanning = true;
        StartCoroutine(ScanForQRCode());
        qrCodeOverlay.SetActive(true);
    }

    IEnumerator ScanForQRCode()
    {
        var barcodeReader = new BarcodeReader
        {
            AutoRotate = true,
            Options = new DecodingOptions { TryHarder = true }
        };

        while (scanning)
        {
            var snap = new Texture2D(camTexture.width, camTexture.height);
            snap.SetPixels32(camTexture.GetPixels32());
            snap.Apply();

            var result = barcodeReader.Decode(snap.GetPixels32(), camTexture.width, camTexture.height);

            if (result != null && result.Text == "DEST_MENU")
            {

                searchInputField.gameObject.SetActive(true);
                searchDropdown.gameObject.SetActive(true);


                scanning = false;
                camTexture.Stop();
                camTexture = null;


                scanZone.gameObject.SetActive(false);
                qrCodeOverlay.SetActive(false);

                Debug.Log("QR code detected: DEST_MENU");
            }

            yield return new WaitForSeconds(0.5f);
        }
    }


    public void OnSearchInputChanged(string inputText)
    {
        if (!string.IsNullOrEmpty(inputText))
        {
            StartCoroutine(FetchFilteredDestinations(inputText));
        }
        else
        {
            searchDropdown.ClearOptions();
            searchDropdown.gameObject.SetActive(false);
        }
    }

    public void OnSearchSubmit(string inputText)
    {
        if (!string.IsNullOrEmpty(inputText))
        {
            StartCoroutine(FetchFilteredDestinations(inputText));
        }
    }

    IEnumerator FetchFilteredDestinations(string query)
    {
        UnityWebRequest request = UnityWebRequest.Get(apiUrl + "?search=" + UnityWebRequest.EscapeURL(query));
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            ParseSearchResults(request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("Error fetching destinations: " + request.error);
        }
    }

    void ParseSearchResults(string data)
    {
        filteredResults.Clear();
        string[] lines = data.Split('\n');
        foreach (string line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                filteredResults.Add(line.Trim());
            }
        }


        searchDropdown.ClearOptions();
        if (filteredResults.Count > 0)
        {
            searchDropdown.AddOptions(filteredResults);
            searchDropdown.gameObject.SetActive(true);
        }
        else
        {
            searchDropdown.gameObject.SetActive(false);
        }
    }

    void OnDropdownSelectionChanged(int selectedIndex)
    {

        Debug.Log("Dropdown selection changed. Selected Index: " + selectedIndex);


        if (selectedIndex >= 0 && selectedIndex < filteredResults.Count)
        {
            string selectedDestination = filteredResults[selectedIndex];
            Debug.Log("Selected Destination: " + selectedDestination);


            searchInputField.text = selectedDestination;


            Debug.Log("Fetching target position for destination: " + selectedDestination);
            StartCoroutine(GetTargetPosition(selectedDestination));
        }
        else
        {
            Debug.LogWarning("Selected index is out of range or invalid.");
        }
    }



    IEnumerator GetTargetPosition(string destination)
    {
        Debug.Log("Sending request to get position for destination: " + destination);
        UnityWebRequest request = UnityWebRequest.Get(apiUrl + "?destination=" + UnityWebRequest.EscapeURL(destination));
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Position data received: " + request.downloadHandler.text);

            string[] positionData = request.downloadHandler.text.Split(',');
            if (positionData.Length == 3 &&
                float.TryParse(positionData[0], out float x) &&
                float.TryParse(positionData[1], out float y) &&
                float.TryParse(positionData[2], out float z))
            {
                Vector3 targetPosition = new Vector3(x, y, z);
                Debug.Log("Parsed Target Position: " + targetPosition);


                if (navigationTargetHandler != null)
                {
                    Debug.Log("Updating navigation target position.");
                    navigationTargetHandler.UpdateTargetPosition(targetPosition);
                }
                else
                {
                    Debug.LogError("NavigationTargetHandler is not assigned.");
                }
            }
            else
            {
                Debug.LogError("Error parsing target position data.");
            }
        }
        else
        {
            Debug.LogError("Error fetching target position: " + request.error);
        }
    }
}