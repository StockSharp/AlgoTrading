# Estratégia de ruptura do indicador Vortex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia transporta o especialista MetaTrader **Vortex Indicator System.mq4** para o StockSharp API de alto nível. A ideia original era
é publicado em *Technical Analysis of Stocks & Commodities* (janeiro de 2010) e se baseia no cruzamento do indicador Vortex para arm bre
ordens de compra na máxima/mínima da vela cruzada. A versão StockSharp mantém o mesmo fluxo de decisão: um cruzamento fecha o
posição oposta, arma um gatilho de rompimento no extremo da barra de cruzamento, e a próxima vela que quebrar esse nível executa o
ordem de mercado.

## Como funciona

1. Uma assinatura de vela única é aberta de acordo com `CandleType`. O fluxo resultante está vinculado a uma instância `VortexIndicator`
Uma vez usando `Bind`, a estratégia sempre recebe valores VI+ e VI- sincronizados para as velas finalizadas.
2. Quando o indicador termina de aquecer, o algoritmo rastreia os valores VI anteriores para detectar as mesmas condições de cruzamento.
sed no especialista MQL: `VI+` cruzando acima de `VI-` ou vice-versa entre as duas últimas velas fechadas.
3. **Fase de configuração** – assim que um cruzamento de alta é detectado, qualquer posição curta aberta é fechada imediatamente e a máxima do
A vela cruzada se torna o gatilho longo pendente. O cruzamento oposto fecha uma posição longa existente e armazena a baixa
dessa barra como o gatilho curto.
4. **Fase de gatilho** – em cada vela finalizada subsequente, a estratégia verifica se o preço de gatilho registrado foi tocado (`Hi
ghPrice` ≥ long trigger or `LowPrice` ≤ gatilho curto). Nesse caso, ele envia uma ordem de mercado dimensionada para achatar a oposição restante.
exposição do site (se o pedido anterior ainda não foi concluído) e abra uma nova posição com `TradeVolume`.
5. Assim que um pedido é acionado, o gatilho correspondente é apagado. Se não ocorrer nenhum rompimento, a configuração permanece ativa até um novo cruzamento
r o substitui.
6. As saídas dependem exclusivamente da lógica de cruzamento: o sinal oposto nivela imediatamente a posição atual e arma um novo b
gatilho reakout, espelhando a implementação MetaTrader.

## Sinais

- **Configuração de alta** – ocorre quando `VI+` estava abaixo ou igual a `VI-` na vela fechada anterior e sobe acima dela no máximo r
recente. O gatilho longo está definido para o máximo dessa vela.
- **Execução de alta** – a próxima vela cuja máxima atingir o gatilho envia uma ordem de compra de mercado usando `TradeVolume` (mais qualquer vo
volume necessário para fechar uma posição curta pendente).
- **Configuração de baixa** – ocorre quando `VI-` estava abaixo ou igual a `VI+` na vela fechada anterior e sobe acima dela no máximo r
recente. O gatilho curto está definido para o mínimo dessa vela.
- **Execução de baixa** – a próxima vela cuja mínima tocar o gatilho envia uma ordem de venda a mercado usando `TradeVolume` (mais o vo
lume necessário para nivelar uma posição longa aberta).

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `VortexLength` | 14 | Período aplicado ao indicador Vortex. |
| `CandleType` | 1 hora | Prazo usado para velas e atualizações de indicadores. |
| `TradeVolume` | 1 | Tamanho da ordem de mercado usado para novas entradas. |

## Notas de implementação

- A estratégia reage apenas a velas **acabadas** para cumprir as diretrizes de conversão. Os rompimentos intrabar são reconhecidos como
assim que uma vela fecha com uma máxima/mínima além do gatilho armazenado.
- Os gatilhos pendentes são limpos em `OnStopped` para que a instância possa ser reiniciada de forma limpa, sem estado restante.
- Ao executar uma ordem de rompimento, o algoritmo aumenta o volume se ainda mantiver uma posição oposta, alcançando o mesmo efeito.
efeito como o especialista MetaTrader, que fechou o pedido ativo antes de abrir o novo.
