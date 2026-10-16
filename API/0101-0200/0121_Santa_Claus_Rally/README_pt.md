# Estratégia Santa Claus Rally
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta implementação negocia **janelas de dias em todos os meses**, não apenas os últimos dias de dezembro e os primeiros de janeiro do efeito natalino tradicional. A alegação anterior de retorno anual de 100% não é validada pelo replay BTC/TON.

### Regras mensais executadas (C# e Python)

Este exemplo **generaliza** o efeito de Natal para **todos os meses**, não apenas dezembro/janeiro. No primeiro candle concluído elegível de 5 minutos de um novo dia 25–31, abre long se estiver sem posição ou encerra short. Nos dias 3–7 encerra long; nos dias 12–18 pode abrir short, se estiver sem posição e `Close < SMA20`. As ações exigem novo dia e respeitam 50 candles concluídos de pausa. Stop nativo de 2% protege posições executadas independentemente do calendário. A rentabilidade citada acima não foi validada no replay BTC/TON.


## Detalhes

- **Critérios de entrada**: gatilhos de efeito calendário
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 5 minute
  - `StopLoss` = 2%
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

