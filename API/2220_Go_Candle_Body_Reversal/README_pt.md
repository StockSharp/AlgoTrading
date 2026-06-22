# Estratégia de Reversão do Corpo de Candle Go
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no indicador Go que calcula a média do tamanho do corpo do candle. Abre uma posição comprada quando o corpo suavizado do candle cruza abaixo de zero após ser positivo, e abre uma posição vendida no cruzamento oposto. As posições existentes são fechadas em sinais opostos.

## Detalhes

- **Critérios de entrada**: mudança de sinal do SMA do corpo (positivo para negativo para comprado, negativo para positivo para vendido)
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: mudança de sinal oposta do SMA do corpo
- **Stops**: Não
- **Valores padrão**:
  - `Period` = 174
  - `CandleType` = 1 hora
- **Filtros**:
  - Categoria: Reversão
  - Direção: Comprado e Vendido
  - Indicadores: SMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
