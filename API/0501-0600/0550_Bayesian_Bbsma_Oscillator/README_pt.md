# Estratégia de Oscilador Bayesian BBSMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia estima a probabilidade de a próxima vela romper para cima ou para baixo usando um modelo Bayesian construído a partir de Bollinger Bands e uma média móvel simples. A confirmação opcional dos indicadores Accelerator e Alligator de Bill Williams pode filtrar os sinais. Quando a probabilidade de uma ruptura ascendente ultrapassa o limiar, uma operação longa é aberta. Uma alta probabilidade de ruptura descendente aciona um curto.

## Detalhes

- **Critérios de entrada**:
  - Comprado quando a probabilidade principal ou ascendente cruza acima de `LowerThreshold` (padrão 15%) e, se habilitado, a confirmação de Bill Williams é altista.
  - Vendido quando a probabilidade principal ou descendente cruza acima do limiar e, se habilitado, a confirmação de Bill Williams é baixista.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Sinal inverso.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `BbSmaPeriod` = 20
  - `BbStdDevMult` = 2.5
  - `AoFast` = 5
  - `AoSlow` = 34
  - `AcFast` = 5
  - `SmaPeriod` = 20
  - `BayesPeriod` = 20
  - `LowerThreshold` = 15
  - `UseBwConfirmation` = false
  - `JawLength` = 13
- **Filtros**:
  - Categoria: Seguidor de tendência probabilístico
  - Direção: Ambos
  - Indicadores: Bollinger Bands, SMA, Awesome Oscillator, Accelerator Oscillator, Alligator
  - Stops: Não
  - Complexidade: Alto
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
