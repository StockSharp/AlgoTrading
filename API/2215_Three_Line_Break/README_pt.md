# Estratégia de Rompimento de Três Linhas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que opera reversões detectadas pelo indicador Three Line Break.
O indicador compara o máximo e mínimo atuais com o máximo mais alto e o mínimo mais baixo das N velas anteriores concluídas.
Um rompimento acima do máximo recente durante uma tendência de baixa sinaliza uma nova tendência de alta e aciona uma entrada comprada; um rompimento abaixo do mínimo recente durante uma tendência de alta aciona uma entrada vendida.
As posições são invertidas em cada sinal.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Downtrend` muda para `Uptrend`
  - Vendido: `Uptrend` muda para `Downtrend`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Sinal oposto (inversão de posição)
- **Stops**: Não
- **Valores padrão**:
  - `LinesBreak` = 3
  - `CandleType` = TimeSpan.FromHours(12).TimeFrame()
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Highest, Lowest (lógica Three Line Break)
  - Stops: Não
  - Complexidade: Básico
  - Período: Swing
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
