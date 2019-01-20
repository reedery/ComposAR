﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

enum EditorMode { Camera, LookingAtObject, SelectedObject, None }

public class EditorToggleButton : MonoBehaviour {

    public float maxScale = 2;

    private XRItem selectedItem;
    private bool isMovingObject;

    public GameObject floor;

    // singleton reference
    public static EditorToggleButton Shared;
    public bool waitingForDuplication = false;

    // selectedobject items 
    public Slider scaleSlider;
    public Slider rotateSlider;
    public Dropdown rotationDropdown;
    public Dropdown scaleDropdown;

    // looking at object options 
    public Button selectButton;
    public Button duplicateButton;
    public Button deleteButton;
    public Button exitButton;

    // camera options 
    public Button takeShotButton;
    public RawImage cameraDisplay;

    public Text selectButtonText;

    private EditorMode currentEditMode = EditorMode.SelectedObject;

    void Awake() {
        EditorToggleButton.Shared = this;
    }

	void Start () {
        scaleSlider.value = 1;
        scaleDropdown.value = 3;
        scaleSlider.maxValue = maxScale;
        scaleSlider.onValueChanged.AddListener(delegate { ScaleValueChange(); });

        rotateSlider.onValueChanged.AddListener(delegate { RotateValueChange(); });
        rotationDropdown.onValueChanged.AddListener(delegate { DropdownValueChange(); });
        scaleDropdown.onValueChanged.AddListener(delegate { ScaleDropdownValueChange(); });

        setEditorMode(EditorMode.None);
	}

    void Update() {
        EditorMode newEditorMode = EditorMode.None;

        // they already have an item, so they are moving an object 
        if (selectedItem != null) {
            newEditorMode = EditorMode.SelectedObject;
        } else {
            XRItem lookingAtItem = XRItemRaycaster.Shared.ItemFocus;

            // if they are looking at an item and not holding one, determine if it is camera/object  
            if (lookingAtItem != null) {
                GameObject lookingAtObject = lookingAtItem.gameObject;

                if (lookingAtObject.transform.name.Contains("camera")) {
                    newEditorMode = EditorMode.Camera;
                } else {
                    newEditorMode = EditorMode.LookingAtObject;
                }
            } else {
                // if they are not looking at an item, they are looking at none
                newEditorMode = EditorMode.None;
            }
        }

        setEditorMode(newEditorMode);
    }

    private void setEditorMode(EditorMode newMode) {
        if (newMode == currentEditMode) {
            // double check if looking at floor, dup/delete button are deleted 
            XRItem lookingAtItem = XRItemRaycaster.Shared.ItemFocus;
            if (lookingAtItem != null && !lookingAtItem.gameObject.transform.name.Contains("Floor")) {
                duplicateButton.gameObject.SetActive(true);
                deleteButton.gameObject.SetActive(true);
            }
            
            return; 
        }

        bool showExitButton = false;

        // selected object controls 
        bool showScaleSlider = false;      
        bool showRotateSlider = false;
        bool showRotateDropdown = false;
        bool showScaleDropdown = false;

        // looking at object controls
        bool showSelectButton = false;
        bool showDuplicateButton = false;
        bool showDeleteButton = false;
        
        bool showTakeShotButton = false;
        bool showRawImage = false;

        if (newMode == EditorMode.None) {
            showExitButton = true;
        } else if (newMode == EditorMode.LookingAtObject) {
            showSelectButton = true;
            showExitButton = true;

            // prevent deleting and duplicating of floor 
            XRItem lookingAtItem = XRItemRaycaster.Shared.ItemFocus;
            if (lookingAtItem != null && !lookingAtItem.gameObject.transform.name.Contains("Floor")) {
                showDuplicateButton = true;
                showDeleteButton = true;
            }
        } else if (newMode == EditorMode.SelectedObject) {
            showSelectButton = true;
            showScaleSlider = true;
            showRotateSlider = true;
            showRotateDropdown = true;
            showScaleDropdown = true;

            // set values back 
            if (selectedItem != null) {
                DropdownValueChange();
                ScaleDropdownValueChange();
            }

            XRItem lookingAtItem = XRItemRaycaster.Shared.ItemFocus;

            if (lookingAtItem != null && lookingAtItem.gameObject.transform.name.Contains("camera")) {
                cameraDisplay.texture = lookingAtItem.gameObject.GetComponent<ComposarCamera>().GetRenderTexture();
                showRawImage = true;
                showTakeShotButton = true;
            }
        } else if (newMode == EditorMode.Camera) {
            showSelectButton = true;
            showDuplicateButton = true;
            showDeleteButton = true;
            showRawImage = true;
            showTakeShotButton = true;

            XRItem lookingAtItem = XRItemRaycaster.Shared.ItemFocus;

            if (lookingAtItem != null) {
                cameraDisplay.texture = lookingAtItem.gameObject.GetComponent<ComposarCamera>().GetRenderTexture();
            }
        }

        scaleSlider.gameObject.SetActive(showScaleSlider);
        rotateSlider.gameObject.SetActive(showRotateSlider);
        rotationDropdown.gameObject.SetActive(showRotateDropdown);
        scaleDropdown.gameObject.SetActive(showScaleDropdown);

        selectButton.gameObject.SetActive(showSelectButton);
        duplicateButton.gameObject.SetActive(showDuplicateButton);
        deleteButton.gameObject.SetActive(showDeleteButton);

        cameraDisplay.gameObject.SetActive(showRawImage);
        takeShotButton.gameObject.SetActive(showTakeShotButton);

        exitButton.gameObject.SetActive(showExitButton);

        currentEditMode = newMode;
    }

    // Menu Toggles

    public void OnClickSpawn() {
        TeleportalInventory.Shared.UseCurrent();
    }

    public void OnClickMove() {
        if (isMovingObject) {
            // place object 
            setIsMoving(false);
        } else {
            // pickup object
            selectedItem = XRItemRaycaster.Shared.ItemFocus;
            setIsMoving(true);
        }
    }

    public void OnClickDelete() {
        XRItem item = XRItemRaycaster.Shared.ItemFocus;

        if (item != null) {
            TeleportalAr.Shared.DeleteItem(item.Id);
        }
    }

    public void OnClickDuplicate() {
        waitingForDuplication = true;

        int lastSlot = TeleportalInventory.Shared.CurrentItem.id;
        int slot = 0;
        for (int i = 0; i < TeleportalInventory.Shared.InventoryArray.Length; i++) {
            if (XRItemRaycaster.Shared.ItemFocus.gameObject.name.Contains(TeleportalInventory.Shared.InventoryArray[i].type)) {
                slot = i;
                break;
            }
        }

        TeleportalInventory.Shared.SetItem(slot);
        TeleportalInventory.Shared.UseCurrent();
        TeleportalInventory.Shared.SetItem(lastSlot);
        // goes to OnDuplication 
    }

    public void OnClickTakeShot() {
        XRItem lookingAtItem = XRItemRaycaster.Shared.ItemFocus;

        if (lookingAtItem != null) {
            lookingAtItem.gameObject.GetComponent<ComposarCamera>().SaveImage();
        }
    }

    public void OnClickExit() {
        // TODO: tom 
    }

    public void OnDuplication(string id, XRItem newItem) {
        XRItem lookingAt = XRItemRaycaster.Shared.ItemFocus;

        if (lookingAt == null) {
            return;
        }
        
        Transform selected = lookingAt.gameObject.transform;
        TeleportalAr.Shared.MoveItem(id, selected.position.x, selected.position.y, selected.position.z, selected.eulerAngles.y, selected.eulerAngles.x);
        
        newItem.gameObject.transform.eulerAngles = new Vector3(selected.eulerAngles.x, selected.eulerAngles.y, selected.eulerAngles.z);
        newItem.gameObject.transform.localScale = new Vector3(selected.localScale.x, selected.localScale.y, selected.localScale.y);
        
        selectedItem = lookingAt;
        setIsMoving(true);
    }

    private void setIsMoving(bool isMoving) {
        if (selectedItem == null) {
            isMoving = false;
            return;
        }

        isMovingObject = isMoving;
        
        float alpha = 1.0f;

        if (isMoving) {
            selectButtonText.text = "Unselect";
            // prevent floor from being added 
            if (!selectedItem.gameObject.name.Equals("Floor")) {
                TeleportalAr.Shared.HoldItem(selectedItem);
            }
            alpha = 0.5f;
        } else {
            selectButtonText.text = "Select";
            TeleportalAr.Shared.ReleaseItem(selectedItem);
            selectedItem.gameObject.transform.SetParent(null);
            selectedItem.gameObject.transform.SetParent(floor.transform);
        }

        for (int i = 0; i < selectedItem.gameObject.transform.childCount; i++) {
            MeshRenderer renderer = selectedItem.gameObject.transform.GetChild(i).gameObject.GetComponent<MeshRenderer>();
            if (renderer != null) {
                renderer.material.color = new Color(1.0f, 1.0f, 1.0f, alpha);
            }
        }
        
        if (!isMovingObject) {
            selectedItem = null;
        }

    }

    // Slider Changes

    public void ScaleValueChange() {
        if (selectedItem == null) {
            return;
        }

        if (ignoreThisChange) {
            ignoreThisChange = false;
            return;
        }

        Vector3 scale = selectedItem.gameObject.transform.localScale;

        Vector3 scaleVector = new Vector3(
                scaleDropdown.value == 0 || scaleDropdown.value == 3 ? scaleSlider.value : scale.x, 
                scaleDropdown.value == 1 || scaleDropdown.value == 3 ? scaleSlider.value : scale.y,
                scaleDropdown.value == 2 || scaleDropdown.value == 3 ? scaleSlider.value : scale.z);

        if (selectedItem.gameObject.name.Equals("Floor")) {
            floor.transform.localScale = scaleVector;
        } else {
            selectedItem.gameObject.transform.localScale = scaleVector;
        }
    }

    public void RotateValueChange() {
        if (selectedItem == null) {
            return; 
        }

        if (ignoreThisChange) {
            ignoreThisChange = false;
            return;
        }

        Vector3 angles = selectedItem.gameObject.transform.eulerAngles;

        Vector3 rotationVector = new Vector3(
                rotationDropdown.value == 0 ? rotateSlider.value : angles.x, 
                rotationDropdown.value == 1 ? rotateSlider.value : angles.y,
                rotationDropdown.value == 2 ? rotateSlider.value : angles.z);

        if (selectedItem.gameObject.name.Equals("Floor")) {
            floor.transform.eulerAngles = rotationVector;
        } else {
            selectedItem.gameObject.transform.eulerAngles = rotationVector;
        }
    }

    bool ignoreThisChange = false;
    public void DropdownValueChange() {
        if (selectedItem == null) {
            return;
        }

        float newValue;
        
        Transform transform = selectedItem.gameObject.name.Equals("Floor") ? floor.transform : selectedItem.gameObject.transform;

        if (rotationDropdown.value == 0) {
            newValue = transform.eulerAngles.x;
        } else if (rotationDropdown.value == 1) {
            newValue = transform.eulerAngles.y;
        } else {
            newValue = transform.eulerAngles.z;
        }

        ignoreThisChange = true;
        rotateSlider.value = newValue;
    }

    public void ScaleDropdownValueChange() {
        if (selectedItem == null) {
            return;
        }

        float newValue;
        
        Transform transform = selectedItem.gameObject.name.Equals("Floor") ? floor.transform : selectedItem.gameObject.transform;

        if (scaleDropdown.value == 0) {
            newValue = transform.localScale.x;
        } else if (scaleDropdown.value == 1) {
            newValue = transform.localScale.y;
        } else if (scaleDropdown.value == 2) {
            newValue = transform.localScale.z;
        } else {
            newValue = transform.localScale.x;
        }

        ignoreThisChange = true;
        scaleSlider.value = newValue;
    }

}