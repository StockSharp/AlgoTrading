# Estratégia Oscilador Pearson's R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Oscilador Pearson's R busca dinamicamente o período em que o preço melhor se ajusta a um canal de regressão linear usando o coeficiente de correlação de Pearson. Quando a correlação atinge o limiar positivo ou negativo especificado, a estratégia forma um canal de regressão e opera rompimentos.

As posições são abertas quando o preço cruza os limites do canal e podem ser fechadas em cruzamentos da linha central. A abordagem se adapta às condições de mercado ajustando automaticamente a janela de análise à correlação mais forte.

## Detalhes

- **Critérios de entrada**:
  - O preço cruza acima da linha de regressão superior → **Comprado**.
  - O preço cruza abaixo da linha de regressão inferior → **Vendido**.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Cruzamento da linha central na direção oposta.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `MinPeriod` = 48
  - `MaxPeriod` = 360
  - `Step` = 12
  - `IdealPositive` = 0.85
  - `IdealNegative` = -0.85
  - `Deviations` = 2
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Pearson's R, Regressão Linear
  - Stops: Nenhum
  - Complexidade: Moderado
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
