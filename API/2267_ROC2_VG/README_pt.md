# Estratégia ROC2 VG
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Recria o expert **Exp_ROC2_VG** do MetaTrader no StockSharp.  
Duas linhas de taxa de variação com períodos e tipos de cálculo configuráveis são comparadas.  
Uma posição comprada é aberta quando a primeira linha cruza abaixo da segunda;  
uma posição vendida é aberta no cruzamento oposto. A opção `Invert` troca as linhas.

## Detalhes

- **Entrada comprada**: anterior up > anterior down E atual up <= atual down.
- **Entrada vendida**: anterior up < anterior down E atual up >= atual down.
- **Saída**: o sinal de reversão inverte imediatamente a posição usando ordens a mercado.
- **Período**: tipo de vela parametrizado, padrão 4 horas.
- **Indicadores**: cada linha pode usar cálculos do tipo Momentum ou ROC:
  - Momentum = `preço - preço anterior`
  - ROC = `((preço / anterior) - 1) * 100`
  - ROCP = `(preço - anterior) / anterior`
  - ROCR = `preço / anterior`
  - ROCR100 = `(preço / anterior) * 100`
- **Parâmetros padrão**:
  - `RocPeriod1` = 8, `RocType1` = Momentum
  - `RocPeriod2` = 14, `RocType2` = Momentum
  - `Invert` = false

A estratégia reverte o tamanho da posição quando os sinais mudam.
