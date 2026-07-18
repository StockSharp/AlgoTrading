# Estratégia Volume EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia opera com base em picos de volume e no Índice de Canal de Commodities (CCI). Abre posições no início de uma nova hora quando o volume da vela anterior supera o da vela anterior a ela por um fator configurável. Os valores do CCI devem cair dentro de bandas específicas para confirmar o sinal.

## Regras
- Apenas uma posição é aberta por vez.
- No início de cada hora:
  - **Entrada comprada** quando:
    - A vela anterior é de alta.
    - Volume anterior > volume prévio × `Factor`.
    - CCI está entre `CciLevel1` e `CciLevel2`.
  - **Entrada vendida** quando:
    - A vela anterior é de baixa.
    - Volume anterior > volume prévio × `Factor`.
    - CCI está entre `CciLevel4` e `CciLevel3`.
- Um stop de rastreamento de `TrailingStop` passos de preço protege os lucros.
- Todas as posições são fechadas quando a hora é igual a 23.

## Parâmetros
- `Factor` – limiar do multiplicador de volume.
- `TrailingStop` – distância de rastreamento em passos de preço.
- `CciLevel1` / `CciLevel2` – limites do CCI para operações compradas.
- `CciLevel3` / `CciLevel4` – limites do CCI para operações vendidas.
- `CandleType` – período de velas utilizado para os cálculos.
