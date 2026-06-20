# Estratégia de Impressões de Dark Pool
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
As Impressões de Dark Pool rastreiam grandes transações fora de bolsa que frequentemente precedem movimentos acentuados assim que a atividade é revelada.
Volume incomum na fita pode sinalizar posicionamento institucional que ainda não impactou o mercado regular.

Os testes indicam um retorno anual médio de aproximadamente 46%. Funciona melhor no mercado de ações.

A estratégia entra na mesma direção das grandes compras ou vendas do dark pool, esperando continuidade quando o restante do mercado reagir.

Um pequeno stop percentual mantém o risco contido e as posições são encerradas se o impulso esperado não se materializar.

## Detalhes

- **Critérios de entrada**: sinal de indicador
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoria: Volume
  - Direção: Ambos
  - Indicadores: Volume
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

