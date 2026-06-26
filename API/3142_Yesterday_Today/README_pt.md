# Estratégia Yesterday Today
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia Yesterday Today reproduz o clássico rompimento do MetaTrader onde o preço de hoje é comparado com o máximo e mínimo de ontem. A estratégia acompanha a última vela diária concluída e observa velas intradíárias para reagir rapidamente quando o preço escapa do intervalo de ontem. Antes de reverter, sempre fecha qualquer exposição oposta, proporcionando um fluxo de trabalho limpo de posição única.

## Visão Geral

- Acompanha o intervalo diário anterior e aguarda o fechamento de uma vela intradíária para rompê-lo.
- Abre posições longas quando o fechamento supera o máximo de ontem; abre posições curtas quando o fechamento cai abaixo do mínimo de ontem.
- Aplica níveis de stop-loss e take-profit a distância fixa expressos em pips. O tamanho do pip se adapta a cotações forex de 3 ou 5 dígitos, assim como na implementação MQL original.
- Os níveis de risco são avaliados em cada vela intradíária concluída usando seu máximo/mínimo para detectar acionamentos de stop-loss ou take-profit.
- Utiliza o framework de proteção integrado para proteger contra problemas inesperados de margem.

## Fluxo de Trabalho

1. Assinar velas diárias e armazenar o máximo/mínimo da última sessão concluída.
2. Assinar velas intradíárias (15 minutos por padrão) para avaliação de sinais.
3. Em cada vela intradíária concluída:
   - Sair imediatamente se a vela violar o stop-loss ou take-profit ativo.
   - Entrar comprado se o fechamento estiver acima do máximo de ontem e não houver posição longa aberta.
   - Entrar vendido se o fechamento estiver abaixo do mínimo de ontem e não houver posição curta aberta.
   - Qualquer posição oposta é fechada primeiro aumentando o volume da ordem de mercado.
4. Sempre que uma nova vela diária for concluída, atualizar o intervalo armazenado para o próximo dia de negociação.

## Parâmetros

- `TradeVolume` — tamanho do lote para novas posições. Ao reverter, a estratégia adiciona automaticamente a exposição oposta para zerar primeiro.
- `StopLossPips` — distância do preço de entrada ao stop protetor, expressa em pips. Um valor de `0` desativa o stop.
- `TakeProfitPips` — distância do preço de entrada ao alvo de lucro, expressa em pips. Um valor de `0` desativa o alvo.
- `SignalCandleType` — tipo de vela intradíária usado para detecção de rompimento (padrão: velas de 15 minutos).

## Detalhes

- **Critérios de entrada**: A vela intradíária fecha acima do máximo de ontem (comprado) ou abaixo do mínimo de ontem (vendido).
- **Comprado/Vendido**: Ambas as direções suportadas.
- **Critérios de saída**: Níveis de stop-loss ou take-profit tocados pelos extremos da vela intradíária.
- **Stops**: Sim, distâncias fixas em pips.
- **Valores padrão**:
  - `TradeVolume` = 1
  - `StopLossPips` = 50
  - `TakeProfitPips` = 50
  - `SignalCandleType` = `TimeSpan.FromMinutes(15).TimeFrame()`
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Price Action
  - Stops: Sim
  - Complexidade: Básico
  - Período: Entradas intradíárias com contexto diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

## Notas

- A estratégia é projetada para um único instrumento. Configure `Security` e `Portfolio` antes de iniciar.
- O tamanho do pip é calculado a partir de `Security.PriceStep` e escalonado automaticamente para símbolos forex de 3 ou 5 decimais, espelhando a lógica original do EA.
- A proteção é ativada em `OnStarted`, para que as salvaguardas globais da conta permaneçam ativas quando a estratégia opera.
