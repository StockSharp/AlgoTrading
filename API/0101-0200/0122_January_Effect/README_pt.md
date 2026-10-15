# Estratégia do Efeito de Janeiro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
O Efeito de Janeiro observa que as ações de pequena capitalização frequentemente superam o desempenho no início do ano, possivelmente devido às vendas por perda fiscal em dezembro.
Os traders tentam capturar essa tendência comprando no final de dezembro e vendendo após as primeiras semanas de janeiro.

Os testes indicam um retorno anual médio de aproximadamente 103%. Funciona melhor no mercado de ações.

A estratégia segue esse cronograma, entrando perto do fim do ano e saindo em meados de janeiro.

Um stop-loss garante que as perdas permaneçam gerenciáveis se o efeito não aparecer.

### Regras mensais executadas (C# e Python)

A implementação é uma adaptação **mensal**, não apenas uma operação em dezembro/janeiro. No primeiro candle de 5 minutos concluído e elegível de um novo dia 1–5, compra sem posição se `Close > SMA20`. Nos dias 14–17 encerra long e pode abrir short se estiver sem posição e `Close < SMA20`. Nos dias 26–31 encerra short. As ações exigem mudança de dia e respeitam 50 candles concluídos de pausa. Stop nativo de 2% protege long e short executados independentemente das datas. A rentabilidade acima não foi confirmada no replay BTC/TON.


## Detalhes

- **Critérios de entrada**: ativadores de efeito de calendário
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 5 minute
  - `StopLossPercent` = 2%
- **Filtros**:
  - Categoria: Sazonalidade
  - Direção: Ambos
  - Indicadores: Sazonalidade
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

