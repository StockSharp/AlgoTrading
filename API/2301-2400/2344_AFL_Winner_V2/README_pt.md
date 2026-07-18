# Estratégia AFL Winner V2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia de exemplo replica a lógica do indicador AFL Winner V2 usando a API de alto nível do StockSharp. O indicador é aproximado por um oscilador estocástico e os sinais são derivados de sua posição relativa e níveis de limiar predefinidos.

## Lógica da estratégia

- Usa um `StochasticOscillator` para emular o comportamento do AFL Winner.
- Abre uma posição comprada quando o oscilador indica forte impulso de alta.
- Abre uma posição vendida quando o oscilador sinaliza forte impulso de baixa.
- Fecha compradas quando o estado de cor cai abaixo da zona neutra.
- Fecha vendidas quando o estado de cor sobe acima da zona neutra.
- Os parâmetros permitem otimizar os períodos do oscilador e os níveis de limiar.

## Parâmetros

| Parâmetro   | Descrição                                           |
|-------------|-----------------------------------------------------|
| `KPeriod`   | Período %K do oscilador estocástico.                |
| `DPeriod`   | Período %D do oscilador estocástico.                |
| `HighLevel` | Limiar superior para sinais de alta.                |
| `LowLevel`  | Limiar inferior para sinais de baixa.               |

## Arquivos

- `CS/AflWinnerV2Strategy.cs` – implementação principal da estratégia.

## Notas

A estratégia opera apenas em velas concluídas e usa proteção automática de posição para evitar exposição indesejada.
