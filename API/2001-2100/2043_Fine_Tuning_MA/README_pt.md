# Estratégia de Ajuste Fino de MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia monitora a inclinação de uma média móvel simples. Após duas barras consecutivas em uma direção, uma reversão da média móvel dispara uma entrada. Um giro ascendente após uma queda abre uma posição comprada, enquanto um giro descendente após uma alta abre uma posição vendida. Sinais opostos fecham as operações existentes.

O sistema foi convertido do consultor MQL "Exp_FineTuningMA" e substitui o indicador personalizado original por uma média móvel simples padrão para maior clareza.

## Detalhes

- **Critérios de entrada**: A MA muda de direção após duas barras.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal oposto ou stop.
- **Stops**: Sim, baseados em percentual.
- **Valores padrão**:
  - `MaLength` = 10
  - `TakeProfitPercent` = 1
  - `StopLossPercent` = 1
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: SMA
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Swing / H4
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
