# Estratégia de Pares
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia de trading de pares compra quando o ativo de referência fecha acima de sua abertura enquanto o símbolo atual forma uma vela de baixa. A posição é fechada quando o preço rompe acima da máxima da vela anterior.

## Detalhes

- **Critérios de entrada**: Ativo de referência em alta e vela de baixa no símbolo atual.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Fechamento acima da máxima anterior.
- **Stops**: Não.
- **Valores padrão**:
  - `CandleType` = 1 minuto
- **Filtros**:
  - Categoria: Trading de pares
  - Direção: Somente comprado
  - Indicadores: Price action
  - Stops: Não
  - Complexidade: Iniciante
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
