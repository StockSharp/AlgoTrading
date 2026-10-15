# Estratégia de Cruzamento de Tripla Média Móvel
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera com base na relação entre três médias móveis: rápida, média e lenta. É uma conversão do especialista MQL **X3MA_EA_V2_0**.

## Lógica de trading

* **Entrada**
  * Quando *EnableEntryMediumSlowCross* é verdadeiro, uma posição comprada é aberta quando a média móvel média cruza acima da lenta. O cruzamento inverso aciona uma entrada vendida.
  * Quando a opção é falsa, a estratégia aguarda que a média rápida cruze a média enquanto ambas permaneçam do mesmo lado da lenta. Posições compradas requerem `fast > medium > slow` e posições vendidas requerem `fast < medium < slow`.
* **Saída**
  * Quando *EnableExitFastSlowCross* é verdadeiro, posições abertas são fechadas quando as médias rápida e lenta se cruzam na direção oposta.

Todos os sinais são avaliados em velas concluídas.

## Parâmetros

| Nome | Descrição |
|------|-----------|
| `FastMaLength` | Período da média móvel rápida. |
| `MediumMaLength` | Período da média móvel média. |
| `SlowMaLength` | Período da média móvel lenta. |
| `EnableEntryMediumSlowCross` | Permitir entradas no cruzamento médio/lento. |
| `EnableExitFastSlowCross` | Fechar posições no cruzamento rápido/lento. |
| `CandleType` | Período dos candles. |

## Notas

A estratégia usa a API de alto nível com `SubscribeCandles` e `Bind`. Os valores dos indicadores são acessados por meio do callback `ProcessCandle` sem usar `GetValue`. A lógica de proteção é habilitada com `StartProtection()` em `OnStarted`.
