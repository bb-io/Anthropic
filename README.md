# Blackbird.io Anthropic

Blackbird is the new automation backbone for the language technology industry. Blackbird provides enterprise-scale automation and orchestration with a simple no-code/low-code platform. Blackbird enables ambitious organizations to identify, vet and automate as many processes as possible. Not just localization workflows, but any business and IT process. This repository represents an application that is deployable on Blackbird and usable inside the workflow editor.

## Introduction

<!-- begin docs -->

A next-generation AI assistant for your tasks, no matter the scale

## Before setting up

Before you can connect you need to make sure that:

- **Anthropic API token connection type**: 
You have an [Anthropic account](https://console.anthropic.com) and have access to the API keys.

- **Microsoft Foundry connection type**:
You have a Microsoft Azure account and a model resource. 
Follow [this guide](https://docs.typingmind.com/manage-and-connect-ai-models/anthropic-models-via-azure#5f87f0dbf0d3488697168438bd45230a)
to set up your Anthropic model. To obtaint credentials, go to Microsoft Foundry -> Playgrounds and click View code. 
Select Key authentication and copy your `Endpoint` and `API key`. Copy your `Deployment name` from the generated code.

![Microsoft Foundry Deployment name](image/README/msfoundry_deployment.png)

- **Amazon Bedrock (AWS Credentials) connection type**: 
You have AWS `Access key` and `Secret key` and the `Region` code where the models were enabled. 
The IAM user must have these policies enabled: `bedrock:InvokeModel`, `bedrock:ListFoundationModels`.

- **Amazon Bedrock (API Key) connection type**: 
You have a Bedrock `API token` generated in the AWS console under Bedrock -> Settings -> API keys, and you have the `Region` code.

**Note**: For Amazon Bedrock connection types, you also need to have access to the specific Anthropic models. 
Go to AWS Bedrock Console -> Model access -> Manage model access and check the boxes for the Anthropic models.

## Connecting

Navigate to Apps and search for Anthropic. Select Anthropic. Click _Add Connection_ and name your connection for 
future reference e.g. 'My connection'.

### Anthropic API token

1. Fill in your `API key`. You can create a new API key under [API keys](https://console.anthropic.com/account/keys). The API key has the shape `sk-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx`.
2. Click _Connect_.

![Anthropic API token](image/README/anthropic_apikey.png)

### Amazon Bedrock (AWS Credentials)

1. Fill in your `Access key`, `Secret key` and `Region`.
2. Click _Connect_.

![Amazon Bedrock AWS Creds](image/README/bedrock_aws.png)

### Amazon Bedrock (API Key)

1. Fill in your `API token` and `Region`.
2. Click _Connect_.

![Amazon Bedrock API Key](image/README/bedrock_apikey.png)

### Microsoft Foundry

1. Fill in your `API Key`, `Endpoint` and `Deployment name`.
2. Click _Connect_.

## Actions

### Chat actions

- **Chat** action has the following input values in order to configure the generated response:
1. Model (All current and available models are listed in the dropdown)
2. Prompt
3. Max tokens to sample
4. Temperature
5. top_p
6. top_k
7. System prompt 
8. Stop sequences

For more in-depth information about action consult the [Anthropic API reference](https://docs.anthropic.com/claude/docs).

### Translation

- **Translate** translate file content retrieved from a CMS or file storage. The output can be used in compatible Blackbird interoperable actions.
- **Translate text** given a text and a locale, tries to create a localized version of the text.

### Editing

- **Edit** Edit a translation. This action assumes you have previously translated content in Blackbird through any translation action. Only looks at translated segments and will change the segment state to reviewed.
- **Edit Text** given a source segment and translated target segment, responds with an edited version of the target segment taking into account typical mistakes.

### Review

- **Review** reviews a translation. This action assumes you have previously translated content in Blackbird through any translation action. Adds a quality score to each unit in the file.
- **Review text** reviews a single text and returns a quality score.

> Note: files are interoperable, under the hood Blackbird may convert to and from bilingual files with metadata but all our Actions are able to accept a lot of different file formats.

### Batch actions

**Note**: Currently, batch actions are not supported for Amazon Bedrock connection types.

- **(Batch) Process XLIFF file** asynchronously process each translation unit in the XLIFF file according to the provided instructions (by default it just translates the source tags) and updates the target text for each unit.
- **(Batch) Post-edit XLIFF file** asynchronously post-edit the target text of each translation unit in the XLIFF file according to the provided instructions and updates the target text for each unit. 
- **(Batch) Get Quality Scores for XLIFF file** asynchronously get quality scores for each translation unit in the XLIFF file.
- **(Batch) Get XLIFF from the batch** get the results of the batch process. This action is suitable only for processing and post-editing XLIFF file and should be called after the async process is completed.
- **(Batch) Get XLIFF from the quality score batch** get the quality scores results of the batch process. This action is suitable only for getting quality scores for XLIFF file and should be called after the async process is completed.

## Token usage

For all actions you can configure the 'Max tokens' optional input. This value limits the number of tokens generated in the response. If left empty, the default value will be the **maximum** number of tokens allowed by the model. For example, Claude Sonnet 4 has a maximum of 64,000 output tokens - leaving this field empty means 64,000 will be used as the value. To limit the tokens generated in the response, set this value to a lower number.

### Temperatures

Anthropic allows you to set the temperature value for and Action you perform in Blackbird. You can customize this value (between 0.0 and 1.0) but we've also added handy labels that are available in the Blackbird UI:

- 0.1 Governed: Maximum predictability.
- 0.3 Balanced: A controlled blend of accuracy and flexibility.
- 0.5 Expressive: Accuracy is preserved, structure is looser (default).
- 0.6 Exploratory: Encourages alternative phrasings and ideas while remaining context-aware.
- 0.8 Experimental: Prioritizes creativity over predictability.

### Model Comparison

The following table compares the key characteristics of Claude models:

| Model | Max Output | Context Window |
|-------|------------|---------------|
| Claude Opus 4.1 | 32,000 tokens | 200K tokens |
| Claude Opus 4 | 32,000 tokens | 200K tokens |
| Claude Sonnet 4 | 64,000 tokens | 200K / 1M (beta) tokens |
| Claude Sonnet 3.7 | 64,000 tokens | 200K tokens |
| Claude Haiku 3.5 | 8,192 tokens | 200K tokens |
| Claude Haiku 3 | 4,096 tokens | 200K tokens |

> Max output is the most important metric to consider when choosing a model for translation tasks, as it determines how much text can be generated in a single response. The context window is also important, especially for tasks that require understanding of larger documents or conversations like glossaries.

## Feedback

Do you want to use this app or do you have feedback on our implementation? Reach out to us using the [established channels](https://www.blackbird.io/) or create an issue.

<!-- end docs -->
