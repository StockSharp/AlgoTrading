# Estratégia de Desaceleração WPR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Desaceleração WPR utiliza o oscilador Williams %R para detectar reversões quando o momentum paralisa perto de níveis extremos. Uma desaceleração ocorre quando o valor atual do Williams %R difere do valor anterior em menos de um ponto. Quando tal desaceleração aparece acima do limiar superior, a estratégia fecha posições vendidas e opcionalmente abre uma posição comprada. Uma desaceleração abaixo do limiar inferior fecha posições compradas e opcionalmente abre uma posição vendida.

## Regras de Entrada e Saída
- **Entrada comprada**: Williams %R está acima de `LevelMax` e a condição de desaceleração é satisfeita. Posições vendidas podem ser fechadas se permitido.
- **Entrada vendida**: Williams %R está abaixo de `LevelMin` e a condição de desaceleração é satisfeita. Posições compradas podem ser fechadas se permitido.
- **Saída comprada**: Acionada por um sinal de entrada vendida quando `BuyPosClose` está habilitado.
- **Saída vendida**: Acionada por um sinal de entrada comprada quando `SellPosClose` está habilitado.

## Parâmetros
- `WprPeriod` – período para calcular Williams %R.
- `LevelMax` – nível de sinal superior (padrão -20) marcando a zona de sobrecompra.
- `LevelMin` – nível de sinal inferior (padrão -80) marcando a zona de sobrevenda.
- `SeekSlowdown` – habilita a detecção de desaceleração entre valores consecutivos de Williams %R.
- `BuyPosOpen` – permitir abertura de posições compradas.
- `SellPosOpen` – permitir abertura de posições vendidas.
- `BuyPosClose` – permitir fechamento de posições compradas em sinais de venda.
- `SellPosClose` – permitir fechamento de posições vendidas em sinais de compra.
- `CandleType` – tipo de vela usado para cálculos do indicador (padrão velas de 6 horas).

## Observações
A estratégia foca exclusivamente na lógica de desaceleração do Williams %R do especialista MQL5 original. Alertas, gerenciamento de dinheiro e outras funcionalidades auxiliares são omitidos por clareza. A funcionalidade de stop-loss e take-profit pode ser adicionada manualmente se necessário.
