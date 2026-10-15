# Estratégia Fractal MFI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma tradução do consultor especialista `Exp_Fractal_MFI.mq5`. Usa o indicador Money Flow Index (MFI) para gerar sinais de trading quando o oscilador cruza níveis superiores e inferiores predefinidos.

## Como funciona
- Calcula o MFI durante um período configurável.
- Quando o valor anterior do MFI estava acima do **Nível Baixo** e o valor atual cai abaixo, um sinal é gerado.
  - No modo **Direct**, isso abre uma posição comprada e opcionalmente fecha vendidas.
  - No modo **Against**, isso abre uma posição vendida e opcionalmente fecha compradas.
- Quando o valor anterior do MFI estava abaixo do **Nível Alto** e o valor atual sobe acima, outro sinal é gerado.
  - No modo **Direct**, isso abre uma posição vendida e opcionalmente fecha compradas.
  - No modo **Against**, isso abre uma posição comprada e opcionalmente fecha vendidas.

Apenas velas concluídas são processadas. A estratégia pode ser configurada para habilitar ou desabilitar a abertura e fechamento de posições compradas ou vendidas separadamente.

## Parâmetros
- `MfiPeriod` – período do cálculo do Money Flow Index.
- `HighLevel` – limiar superior para o MFI.
- `LowLevel` – limiar inferior para o MFI.
- `CandleType` – período das velas usado nos cálculos.
- `Trend` – escolher `Direct` para operar na direção do indicador ou `Against` para inverter os sinais.
- `BuyPosOpen` / `SellPosOpen` – permitir abertura de posições compradas ou vendidas.
- `BuyPosClose` / `SellPosClose` – permitir fechamento de posições existentes em sinais opostos.

## Notas
Esta versão em C# foca no uso da API de alto nível e não implementa as regras originais de gestão de capital nem os níveis de stop do código MQL.
