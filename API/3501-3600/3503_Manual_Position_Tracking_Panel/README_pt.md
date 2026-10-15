# Estratégia do Painel de Rastreamento de Posição Manual
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

O consultor especialista MQL5 original fornecia um painel de controle visual que permitia ao trader gerenciar manualmente até cinco posições longas e cinco posições curtas. Os botões dentro do painel excluíram os níveis de lucro existentes, recalcularam novos preços de lucro da entrada ou os moveram para o ponto de equilíbrio dos ingressos selecionados. A porta StockSharp automatiza essas ações de proteção sem a interface visual. A estratégia monitora a posição agregada do símbolo configurado e mantém dinamicamente uma ordem protetora de lucro que reflete o fluxo de trabalho do painel.

Principais etapas de automação:

- Faça um take-profit ao preço de entrada mais/menos uma distância configurável de MetaTrader pip quando uma posição aparecer.
- Opcionalmente, empurre o take-profit para o preço médio de entrada assim que o mercado se mover na direção favorável pelo número solicitado de pips, bloqueando efetivamente uma saída de equilíbrio.
- Respeite as distâncias de congelamento/parada do corretor quando elas forem publicadas por meio de dados de Nível 1 ou aproxime-as usando o spread atual e um multiplicador controlado pelo usuário.
- Cancelar a ordem de proteção sempre que o gerenciamento for desativado ou a posição for fechada, mantendo o comportamento consistente com o botão “Excluir TP” do painel.

A classe depende exclusivamente de métodos StockSharp API de alto nível (`SubscribeLevel1`, `SellLimit`, `BuyLimit`, `ReRegisterOrder`, etc.) e usa normalização automática de volume/preço para que possa ser anexada a qualquer instrumento suportado pelo conector.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| ** Distância Take Profit (pips) ** | Distância de MetaTrader pip adicionada ao preço de entrada ao criar o take-profit protetor. |
| **Ativar o lucro com base em entradas** | Permite a colocação automática do take-profit derivado do preço de entrada. Quando desativada, a estratégia reage apenas a solicitações de ponto de equilíbrio. |
| **Ativar ponto de equilíbrio** | Move o lucro de volta para o preço médio de entrada assim que o gatilho do ponto de equilíbrio for satisfeito. |
| **Gatilho do ponto de equilíbrio (pips)** | Movimento favorável mínimo (em MetaTrader pips) necessário antes que o ponto de equilíbrio seja aplicado. Um valor de `0` aplica-o imediatamente. |
| **Gerenciar posições longas** | Quando `true` o lado longo da posição agregada é processado. |
| **Gerenciar posições curtas** | Quando `true` o lado curto da posição agregada é processado. |
| **Remover take-profit quando desativado** | Cancela a ordem de proteção se as condições de gerenciamento não forem atendidas (semelhante ao botão Excluir TP original). |
| **Ações de gerenciamento de registros** | Ativa o registro informativo para cada ação de criação, modificação ou cancelamento executada pelo algoritmo. |
| **Multiplicador de distância de congelamento** | Multiplicador usado para aproximar distâncias de congelamento/parada do spread atual quando a exchange não publica níveis explícitos. |

## Sinais e regras de execução

1. No início, a estratégia assina atualizações de Nível 1 para rastrear os melhores preços de compra/venda, além dos níveis opcionais de congelamento e parada expostos pelo gateway.
2. Sempre que surge uma nova negociação, a posição global muda ou chegam novos dados de nível 1, a estratégia reavalia a lógica de proteção.
3. Se nenhuma posição estiver aberta, qualquer ordem de take-profit existente será cancelada.
4. Se uma posição estiver ativa e o lado correspondente estiver habilitado:
   - A meta base é o preço de entrada alterado pela distância de take-profit configurada (se habilitada).
   - Quando o ponto de equilíbrio está ativado e o preço de mercado atual se moveu o suficiente, a meta é fixada no preço médio de entrada.
   - A meta é ajustada para respeitar as distâncias de congelamento/parada, comparando-a com a cotação atual do mercado.
   - O preço e o volume são normalizados via `PriceStep`/`VolumeStep` e, em seguida, uma ordem com limite é registrada ou registrada novamente no lado oposto.
5. Se a configuração desabilitar o gerenciamento para o lado detectado, o take-profit existente será removido quando **Remover take-profit quando desabilitado** for `true`.

## Notas de gerenciamento de risco

- O algoritmo gerencia apenas ordens de lucro. Níveis de stop-loss, lógica móvel ou saídas parciais estão fora do seu escopo.
- Como o painel original funcionou com MetaTrader "pips" (pontos), a estratégia calcula o tamanho do pip automaticamente a partir de `PriceStep` e a precisão do instrumento para permanecer compatível com os símbolos Forex.
- As distâncias de congelamento/parada de nível 1 são respeitadas quando disponíveis. Caso a corretora não os envie, o parâmetro multiplicador permite ao usuário criar um buffer de segurança a partir do spread ao vivo, evitando modificações rejeitadas.
- A estratégia não cria novas entradas no mercado; ele foi projetado para ser anexado a sistemas de negociação discricionários ou externos que já gerenciam a execução de ordens.

## Dicas de uso

1. Anexe a estratégia ao instrumento que você deseja supervisionar e certifique-se de que o conector forneça informações de Nível 1.
2. Configure a distância pip para que corresponda ao alvo de proteção usado anteriormente em MetaTrader.
3. Ative o módulo de equilíbrio quando desejar que a proteção bloqueie os lucros quando uma posição se tornar favorável. Deixe o gatilho em zero para um ponto de equilíbrio imediato.
4. Desative o gerenciamento de um lado (longo ou curto) se quiser manter o controle discricionário sobre essa direção.
5. Monitore a saída do log quando **Ações de gerenciamento de log** estiver ativo para verificar se os pedidos foram criados ou ajustados conforme o esperado.
