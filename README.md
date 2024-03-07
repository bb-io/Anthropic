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

There is only one action - **Create completion** <br/>
Action has the following input values in order to configure the generated response:

- Model (Claude 2.1, Claude 2, Claude Instant, Claude 3 Sonnet, Claude 3 Opus)
- Prompt
- Max tokens to sample
- Temperature
- top_p
- top_k
- System prompt
- Stop sequences

For more in-depth information about action consult the [Anthropic API reference](https://docs.anthropic.com/claude/docs).

## Feedback

Do you want to use this app or do you have feedback on our implementation? Reach out to us using the [established channels](https://www.blackbird.io/) or create an issue.

<!-- end docs -->
