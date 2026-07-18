# Estratégia de Operador de Divergência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia compara duas médias móveis simples (SMA) e opera com base na divergência entre elas.

Utiliza a diferença entre a SMA rápida e a SMA lenta da vela anterior como medida de divergência. Se esta divergência for positiva mas dentro de um intervalo especificado, a estratégia abre uma posição comprada. Se a divergência for negativa e dentro do intervalo espelhado, abre uma posição vendida. O risco é gerenciado por níveis opcionais de stop-loss e take-profit.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: SMA rápida anterior - SMA lenta anterior >= `DvBuySell` e <= `DvStayOut`.
  - **Vendido**: SMA rápida anterior - SMA lenta anterior <= `-DvBuySell` e >= `-DvStayOut`.
- **Critérios de saída**: As posições são encerradas via stop-loss ou take-profit, se configurados.
- **Stops**: Suportados via `StartProtection` com deslocamentos de preço absolutos.
- **Valores padrão**:
  - `FastPeriod` = 7
  - `SlowPeriod` = 88
  - `DvBuySell` = 0.0011
  - `DvStayOut` = 0.0079
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: SMA
  - Stops: Opcional
  - Complexidade: Básico
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim
  - Nível de risco: Médio
