# Blackbird.io Anthropic

Blackbird is the new automation backbone for the language technology industry. Blackbird provides enterprise-scale automation and orchestration with a simple no-code/low-code platform. Blackbird enables ambitious organizations to identify, vet and automate as many processes as possible. Not just localization workflows, but any business and IT process. This repository represents an application that is deployable on Blackbird and usable inside the workflow editor.

## Introduction

<!-- begin docs -->

A next-generation AI assistant for your tasks, no matter the scale

## Before setting up

Before you can connect you need to make sure that:

- You have an [Anthropic account](https://console.anthropic.com) and have access to the API keys.

## Connecting

1. Navigate to apps and search for Anthropic. If you cannot find Anthropic then click _Add App_ in the top right corner, select Anthropic and add the app to your Blackbird environment.
2. Click _Add Connection_.
3. Name your connection for future reference e.g. 'My Anthropic connection'.
4. Fill in your API key. You can create a new API key under [API keys](https://console.anthropic.com/account/keys). The API key has the shape `sk-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx`.
5. Click _Connect_.

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

### XLIFF actions

- **Translate** Translate file content retrieved from a CMS or file storage. The output can be used in compatible actions.
- **Edit** Edit a translation. This action assumes you have previously translated content in Blackbird through any translation action.
- **Review** Review translation. This action assumes you have previously translated content in Blackbird through any translation action.
- **Process XLIFF** processes the XLIFF file and returns updated XLIFF with the translated content. By default it will translate source and place the translation in the target field. But you can modify behavior by providing your custom `prompt`. Deprecated: use the 'Translate' action instead.
- **Post-edit XLIFF file** action is used to post-edit the XLIFF file. Deprecated: use the 'Edit' action instead.
- **Get Quality Scores for XLIFF file** action is used to get quality scores for the XLIFF file by adding `extradata` attribute to the translation unit of the file. Default criteria are `fluency`, `grammar`, `terminology`, `style`, and `punctuation`, but you can add your own by filling `prompt` optional input. Deprecated: use the 'Review' action instead.

### Batch actions

- **(Batch) Process XLIFF file** asynchronously process each translation unit in the XLIFF file according to the provided instructions (by default it just translates the source tags) and updates the target text for each unit.
- **(Batch) Post-edit XLIFF file** asynchronously post-edit the target text of each translation unit in the XLIFF file according to the provided instructions and updates the target text for each unit. 
- **(Batch) Get Quality Scores for XLIFF file** asynchronously get quality scores for each translation unit in the XLIFF file.
- **(Batch) Get XLIFF from the batch** get the results of the batch process. This action is suitable only for processing and post-editing XLIFF file and should be called after the async process is completed.
- **(Batch) Get XLIFF from the quality score batch** get the quality scores results of the batch process. This action is suitable only for getting quality scores for XLIFF file and should be called after the async process is completed.

## Feedback

Do you want to use this app or do you have feedback on our implementation? Reach out to us using the [established channels](https://www.blackbird.io/) or create an issue.

<!-- end docs -->
