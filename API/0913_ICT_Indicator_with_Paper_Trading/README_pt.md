# Estratégia ICT com Indicador e Paper Trading
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia armazena as máximas e mínimas dos blocos de ordens e assume posições compradas quando o preço de fechamento cruza acima da última máxima do bloco de ordens. A posição comprada é encerrada quando a mínima armazenada do bloco de ordens cruza acima do preço.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: preço de fechamento cruza acima da última máxima do bloco de ordens.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - Encerrar comprado quando a mínima do bloco de ordens cruza acima do preço.
- **Stops**: Não.
- **Valores padrão**:
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Somente comprado
  - Indicadores: Price action
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
