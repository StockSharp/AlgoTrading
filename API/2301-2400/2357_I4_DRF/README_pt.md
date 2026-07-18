# Estratégia I4 DRF
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no indicador personalizado I4 DRF. Ela compara a direção das máximas e mínimas recentes dos candles e gera um valor entre -100 e +100. As ações de trading dependem das transições de cor deste indicador e do modo selecionado.

## Detalhes

- **Critérios de entrada**:
  - Modo `Direct`: abrir comprado quando o indicador muda de positivo para negativo; abrir vendido quando muda de negativo para positivo.
  - Modo `NotDirect`: abrir comprado em uma mudança de negativo para positivo; abrir vendido em uma mudança de positivo para negativo.
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - As posições são fechadas quando o sinal oposto aparece.
- **Stops**: Nenhum
- **Valores padrão**:
  - `Period` = 11
  - `SignalBar` = 1
  - `TrendMode` = Direct
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: I4 DRF
  - Stops: Não
  - Complexidade: Básico
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
