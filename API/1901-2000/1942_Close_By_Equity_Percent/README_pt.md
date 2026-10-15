# Estratégia de Fechamento por Percentual de Capital
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia de gestão de risco monitora o capital do portfólio e fecha qualquer posição aberta quando o capital cresce acima do saldo atual multiplicado por um multiplicador definido pelo usuário. Foi projetada para garantir lucros assim que o valor da conta atinge uma porcentagem desejada acima do valor base.

A estratégia realiza verificações periódicas usando candles e não gera entradas de negociação por conta própria; ela apenas gerencia uma posição existente. Após o fechamento, o saldo de referência é atualizado, permitindo que o processo se repita para negociações subsequentes.

## Detalhes

- **Critérios de entrada**: Nenhum (gerencia a posição existente).
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: Capital maior que `balance * EquityPercentFromBalance`.
- **Stops**: Não.
- **Valores padrão**:
  - `EquityPercentFromBalance` = 1.2m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Gestão de risco
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (1m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo

