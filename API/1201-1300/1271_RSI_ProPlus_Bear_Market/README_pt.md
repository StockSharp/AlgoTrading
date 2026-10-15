# Estratégia RSI ProPlus de Mercado em Baixa
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia compra quando o RSI cruza acima de um nível especificado e sai com um percentual fixo a partir do preço de entrada. Foi projetada para condições de mercado em baixa com expectativa de recuperações rápidas.

## Detalhes

- **Critérios de entrada**: RSI cruza acima do nível
- **Comprado/Vendido**: Comprado
- **Critérios de saída**: Take profit a um percentual do preço de entrada
- **Stops**: Não
- **Valores padrão**:
  - `RSI Period` = 11
  - `RSI Level` = 8
  - `Take Profit %` = 0.11
- **Filtros**:
  - Categoria: Momentum
  - Direção: Comprado
  - Indicadores: RSI
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
