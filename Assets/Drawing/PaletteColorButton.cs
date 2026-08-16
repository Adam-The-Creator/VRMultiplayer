using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PaletteColorButton : MonoBehaviour
{
    private enum Side
    {
        Left,
        Right
    }

    // Auto initialized variables
    [Header("!! DONT INITIALIZE MANUALLY !!")]
    [SerializeField] private Button         colorButton;
    [SerializeField] private Color          buttonColor;
    [SerializeField] private Brush          brush;
    [SerializeField] private bool           isGradEndColor = false;
    [SerializeField] private GameObject     drawingHandMenuObject;
    [SerializeField] private Side           controllerSide = Side.Left;
    [SerializeField] private Slider         opacitySlider;
    [SerializeField] private TMP_InputField R_ValueInput;
    [SerializeField] private TMP_InputField G_ValueInput;
    [SerializeField] private TMP_InputField B_ValueInput;
    [SerializeField] private TMP_InputField A_ValueInput;
    [SerializeField] private float          alphaValue = 1f;

    void Start()
    {
        colorButton = gameObject.GetComponent<Button>();
        buttonColor = colorButton.GetComponent<Image>().color;

        if (gameObject.transform.parent.parent.parent.parent.parent.name.ToLower().Contains("left"))
        {
            if (GameObject.FindGameObjectsWithTag("BrushLeft")[0].TryGetComponent<Brush>(out var brushRef)) brush = brushRef;
            controllerSide = Side.Left;
        }
        else if (gameObject.transform.parent.parent.parent.parent.parent.name.ToLower().Contains("right"))
        {
            if (GameObject.FindGameObjectsWithTag("BrushRight")[0].TryGetComponent<Brush>(out var brushRef)) brush = brushRef;
            controllerSide = Side.Right;
        }
        else Debug.LogError("Naming convention: Parent (on 5th level) object of ColorButton shall contain the name of the side of the controller.");

        if (colorButton.gameObject.transform.parent.parent.parent.name.ToLower().Contains("colorspalette")) isGradEndColor = false;
        else if (colorButton.gameObject.transform.parent.parent.parent.name.ToLower().Contains("gradcolorsendpalette")) isGradEndColor = true;
        else Debug.LogError($"Naming convention: Parent (on 3rd level) ({colorButton.gameObject.transform.parent.parent.parent.name}) object of ColorButton" +
            $" shall contain either 'ColorsPalette' or 'GradColorsEndPalette' in its name.");

        GameObject colorPaletteUI = transform.parent.parent.parent.gameObject;
        Debug.Log($"Color Palette: {colorPaletteUI.name}");
        opacitySlider = colorPaletteUI.transform.Find("Panel").transform.Find("Label_Opacity").transform.Find("OpacitySlider").GetComponent<Slider>();
        R_ValueInput = colorPaletteUI.transform.Find("Panel").transform.Find("Label_RGBA").transform.Find("InputField_R").GetComponent<TMP_InputField>();
        G_ValueInput = colorPaletteUI.transform.Find("Panel").transform.Find("Label_RGBA").transform.Find("InputField_G").GetComponent<TMP_InputField>();
        B_ValueInput = colorPaletteUI.transform.Find("Panel").transform.Find("Label_RGBA").transform.Find("InputField_B").GetComponent<TMP_InputField>();
        A_ValueInput = colorPaletteUI.transform.Find("Panel").transform.Find("Label_RGBA").transform.Find("InputField_A").GetComponent<TMP_InputField>();
        Debug.Log($"Opacity Slider: {opacitySlider.name}");
        Debug.Log($"R Input: {R_ValueInput.name}\nG Input: {G_ValueInput.name}\nB Input: {B_ValueInput.name}\nA Input: {A_ValueInput.name}");
        R_ValueInput.text = "0";
        G_ValueInput.text = "0";
        B_ValueInput.text = "0";
        A_ValueInput.text = alphaValue.ToString();

        opacitySlider.onValueChanged.AddListener(opacityValue =>
        {
            if (controllerSide == Side.Left)
            {
                DrawingHandMenuLeft drawingHandMenuLeft = drawingHandMenuObject.GetComponent<DrawingHandMenuLeft>();
                if (A_ValueInput != null) A_ValueInput.text = opacityValue.ToString("F2");
                brush.brushStartColor.a = opacityValue;
                if (!drawingHandMenuLeft.IsGradientModeEnabled()) brush.brushEndColor.a = opacityValue;
            }
            else
            {
                DrawingHandMenuRight drawingHandMenuRight = drawingHandMenuObject.GetComponent<DrawingHandMenuRight>();
                if (A_ValueInput != null) A_ValueInput.text = opacityValue.ToString("F2");
                brush.brushStartColor.a = opacityValue;
                if (!drawingHandMenuRight.IsGradientModeEnabled()) brush.brushEndColor.a = opacityValue;
            }
        });

        drawingHandMenuObject = colorButton.transform.parent.parent.parent.parent.gameObject;

        colorButton.onClick.AddListener(() =>
        {
            if (controllerSide == Side.Left)
            {
                DrawingHandMenuLeft drawingHandMenuLeft = drawingHandMenuObject.GetComponent<DrawingHandMenuLeft>();
                if (!drawingHandMenuLeft.IsGradientModeEnabled())
                {
                    brush.SetStartColor(buttonColor);
                    brush.SetEndColor(buttonColor);
                }
                else
                {
                    if (isGradEndColor) brush.SetEndColor(buttonColor);
                    else brush.SetStartColor(buttonColor);
                }
            }
            else if (controllerSide == Side.Right)
            {
                DrawingHandMenuRight drawingHandMenuRight = drawingHandMenuObject.GetComponent<DrawingHandMenuRight>();
                if (!drawingHandMenuRight.IsGradientModeEnabled())
                {
                    brush.SetStartColor(buttonColor);
                    brush.SetEndColor(buttonColor);
                }
                else
                {
                    if (isGradEndColor) brush.SetEndColor(buttonColor);
                    else brush.SetStartColor(buttonColor);
                }
            }

            R_ValueInput.text = Mathf.RoundToInt(buttonColor.r * 255).ToString();
            G_ValueInput.text = Mathf.RoundToInt(buttonColor.g * 255).ToString();
            B_ValueInput.text = Mathf.RoundToInt(buttonColor.b * 255).ToString();
        });
    }
}
