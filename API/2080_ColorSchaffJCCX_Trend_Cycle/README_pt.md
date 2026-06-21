# Estratégia de Ciclo de Tendência Color Schaff JCCX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma conversão em C# do especialista MQL5 `Exp_ColorSchaffJCCXTrendCycle`.
Emprega o oscilador **Schaff Trend Cycle (STC)** construído sobre o algoritmo JCCX.

## Lógica de negociação

* Calcular o Schaff Trend Cycle em cada vela concluída.
* Quando o oscilador cai abaixo do `High Level` após ter estado acima dele, uma posição comprada é aberta e as posições vendidas são fechadas.
* Quando o oscilador sobe acima do `Low Level` após ter estado abaixo dele, uma posição vendida é aberta e as posições compradas são fechadas.

## Parâmetros

| Nome | Descrição |
|------|-------------|
| Fast JCCX | Período JCCX rápido usado no indicador. |
| Slow JCCX | Período JCCX lento usado no indicador. |
| Smoothing | Fator de suavização JJMA para JCCX. |
| Phase | Valor de fase JJMA. |
| Cycle | Comprimento do ciclo para o cálculo do Schaff Trend. |
| High Level | Nível de gatilho superior do oscilador. |
| Low Level | Nível de gatilho inferior do oscilador. |
| Open Long | Permitir abertura de posições compradas. |
| Open Short | Permitir abertura de posições vendidas. |
| Close Long | Permitir fechamento de posições compradas existentes. |
| Close Short | Permitir fechamento de posições vendidas existentes. |

## Notas

A estratégia usa a API de alto nível do StockSharp e subscreve dados de velas. Reage apenas a velas **concluídas**. O gerenciamento de dinheiro e o controle de risco são mantidos simples para fins de demonstração.
