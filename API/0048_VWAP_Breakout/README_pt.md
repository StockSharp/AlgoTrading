# VWAP Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
VWAP Breakout busca que o preço cruze o Preço Médio Ponderado por Volume pelo lado oposto. Um rompimento acima do VWAP sinaliza pressão altista, enquanto uma queda abaixo do VWAP sinaliza sentimento baixista.

Os testes indicam um retorno anual médio de aproximadamente 181%. Funciona melhor no mercado de criptomoedas.

A estratégia aguarda um fechamento do outro lado do VWAP e então opera nessa direção. As saídas ocorrem quando o preço reverte e cruza novamente o VWAP.

Como o VWAP representa o preço médio de transação, os rompimentos frequentemente geram movimentos de momentum.

## Detalhes

- **Critérios de entrada**: O preço fecha do lado oposto do VWAP.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: O preço cruza de volta pelo VWAP ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: VWAP
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

