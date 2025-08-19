
# Tripo's Plugin for Unity

The Tripo's Plugin for Unity provides a seamless integration of Tripo3D’s powerful features directly into the Unity development environment. This plugin enables Unity developers and creators to effortlessly utilize Tripo3D’s tools, including text-to-model generation and image-to-model generation, for a variety of applications in 3D content creation and game development.


## Install

Follow these steps to install the **Tripo for Unity** into your Unity project. Ensure you are using Unity version **2020.3 or higher**.


---

# Tripo for Unity Installation Guide

## Step 1: Import the Plugin Package
1. Locate the **`Packages`** folder in your Unity project directory.
2. Extract the **`com.unity.vast.tripoforunity@1.0.1`** package and place its contents into the **`Packages`** folder.

## Step 2: Verify Installation
Once the package is placed in the **`Packages`** folder and you open your Unity project, the **`Tripo for Unity`** plugin will be installed. You can verify the installation by:

- Checking for the **Editor** and **Runtime** folders in the **Tripo for Unity directory** within your **Packages** folder.
- Opening the **TripoPlugin Window**:
  - Navigate to **`Window > TripoPlugin`** to confirm the plugin is installed and accessible.
---
## Usage Guide - Editor

###  Set Up Your API Key
1. Navigate to **`Window > TripoPlugin`**.
2. In the popup text inputfield, input your API key (start with `tsk_`) and click  (**✔**) to confirm.
3. Once your API key is validated, all Tripo3D features will become available.

### Generate a 3D Model Using Text
1. In **Text to Model** module, Enter a description of the model in the text inputfield.
2. Click the **Generate** button to create the model.

### Generate a 3D Model Using Image
1. In **Image to Model** module, Click the box to upload and preview an image from your computer.
2. Click the **Generate** button to create the model.

## Usage Guide - Runtime
### Add TripoRuntimeCore to Your Scene
1. create a new empty GameObject.
3. Locate the script at **`Packages/com.unity.vast.tripoforunity/Runtime/Scripts/TripoRuntimeCore.cs`**
2. Attach the **`TripoRuntimeCore`** script to the empty GameObject.
### Core Functionalities
With the **`TripoRuntimeCore`** script attached, you can directly call Tripo's features from your scripts. Additionally, you can adjust various **public variables** of the **`TripoRuntimeCore`** component to customize the generation process.

Below are examples of how to use some of the core functionalities in Runtime Mode. These methods can be directly implemented or found in the **`TripoSimpleUI_Manager`** script for easy integration.

### Setup API Key
```csharp
 GetComponent<TripoRuntimeCore>().set_api_key("Your API Key");
```
### Generate 3D Model from Text
```csharp
GetComponent<TripoRuntimeCore>().textPrompt = "Your Text Prompts";
GetComponent<TripoRuntimeCore>().Text_to_Model_func();
```
### Generate 3D Model from Image
```csharp
GetComponent<TripoRuntimeCore>().imagePath = "Your Image's Path";
GetComponent<TripoRuntimeCore>().Image_to_Model_func();
```
### Handle Model Download Completion
```csharp
void Start()
{
    GetComponent<TripoRuntimeCore>().OnDownloadComplete.AddListener(OnFbxDownloadComplete);
}

void OnFbxDownloadComplete(string GltfURL)
{
    Debug.Log($"Model generated completed at: {GltfURL}");
}
```


Enjoy create and manage 3D models with the Tripo's Plugin for Unity. 🎉

